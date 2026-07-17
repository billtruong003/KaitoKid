#if STDB_BINDINGS
// Requires module_bindings (auto-generated SpacetimeDB bindings)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BillGameCore;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace SpumOnline.UI
{
    /// <summary>
    /// Chat box UI that displays messages from the chat_message table.
    /// Supports sending messages via the SendChat reducer, color-coding by
    /// channel, and maintains a rolling buffer of the most recent messages.
    /// </summary>
    public class ChatUI : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------

        [Header("Display")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private TMP_Text messageDisplay;
        [SerializeField] private RectTransform contentRect;

        [Header("Input")]
        [SerializeField] private TMP_InputField chatInput;
        [SerializeField] private Button sendButton;

        [Header("Settings")]
        [SerializeField] private int maxMessages = NetworkConfig.MAX_CHAT_MESSAGES;

        [Header("Channel Colors")]
        [SerializeField] private Color globalColor = Color.white;
        [SerializeField] private Color systemColor = Color.yellow;
        [SerializeField] private Color adminColor = Color.red;
        [SerializeField] private Color partyColor = new Color(0.4f, 0.8f, 1f);
        [SerializeField] private Color whisperColor = new Color(0.9f, 0.5f, 0.9f);

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------

        private readonly List<string> _messageBuffer = new List<string>();
        private bool _autoScroll = true;

        // -------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------

        private void Start()
        {
            // Setup UI
            if (sendButton != null)
            {
                sendButton.onClick.AddListener(OnSendClicked);
            }

            if (chatInput != null)
            {
                chatInput.onSubmit.AddListener(OnInputSubmit);
            }

            // Clear initial display
            if (messageDisplay != null)
            {
                messageDisplay.text = "";
            }

            // Listen for chat events
            if (Bill.IsReady)
            {
                Bill.Events.Subscribe<ChatMessageReceivedEvent>(OnChatMessageReceived);
            }

            // Register direct table callback as well for reliable delivery
            RegisterTableCallback();
        }

        private void OnDestroy()
        {
            if (Bill.IsReady)
            {
                Bill.Events.Unsubscribe<ChatMessageReceivedEvent>(OnChatMessageReceived);
            }

            UnregisterTableCallback();
        }

        private void Update()
        {
            // Focus chat input on Enter key press when not already focused
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (chatInput != null && !chatInput.isFocused)
                {
                    chatInput.ActivateInputField();
                    chatInput.Select();
                }
            }

            // ESC to unfocus
            if (Input.GetKeyDown(KeyCode.Escape) && chatInput != null && chatInput.isFocused)
            {
                chatInput.DeactivateInputField();
            }
        }

        // -------------------------------------------------------
        // Table Callback
        // -------------------------------------------------------

        private void RegisterTableCallback()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null) return;

            gm.Connection.Db.ChatMessage.OnInsert += OnChatTableInsert;

            // Load existing messages from the subscription cache
            foreach (var msg in gm.Connection.Db.ChatMessage.Iter())
            {
                AppendMessage(msg.SenderName, msg.Content, msg.Channel);
            }
        }

        private void UnregisterTableCallback()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null) return;

            gm.Connection.Db.ChatMessage.OnInsert -= OnChatTableInsert;
        }

        private void OnChatTableInsert(EventContext ctx, ChatMessage row)
        {
            AppendMessage(row.SenderName, row.Content, row.Channel);
        }

        // -------------------------------------------------------
        // Event Handler
        // -------------------------------------------------------

        private void OnChatMessageReceived(ChatMessageReceivedEvent evt)
        {
            // This is an alternative path via BillGameCore events.
            // We use the direct table callback above for the primary path to avoid duplicates.
            // This handler is here for any external systems that fire ChatMessageReceivedEvent manually.
        }

        // -------------------------------------------------------
        // Message Display
        // -------------------------------------------------------

        private void AppendMessage(string sender, string content, int channel)
        {
            Color channelColor = GetChannelColor(channel);
            string colorHex = ColorUtility.ToHtmlStringRGB(channelColor);

            string formattedMessage;

            // Channel 1 = system messages (no sender)
            if (channel == 1)
            {
                formattedMessage = $"<color=#{colorHex}>[System] {content}</color>";
            }
            else if (channel == 2)
            {
                formattedMessage = $"<color=#{colorHex}>[Admin] {sender}: {content}</color>";
            }
            else
            {
                formattedMessage = $"<color=#{colorHex}>{sender}: {content}</color>";
            }

            _messageBuffer.Add(formattedMessage);

            // Trim to max
            while (_messageBuffer.Count > maxMessages)
            {
                _messageBuffer.RemoveAt(0);
            }

            // Rebuild display text
            RebuildDisplay();

            // Auto-scroll to bottom
            if (_autoScroll && scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void RebuildDisplay()
        {
            if (messageDisplay == null) return;

            messageDisplay.text = string.Join("\n", _messageBuffer);
        }

        private Color GetChannelColor(int channel)
        {
            return channel switch
            {
                0 => globalColor,     // Global
                1 => systemColor,     // System
                2 => adminColor,      // Admin
                3 => partyColor,      // Party
                4 => whisperColor,    // Whisper
                _ => globalColor
            };
        }

        // -------------------------------------------------------
        // Send
        // -------------------------------------------------------

        private void OnInputSubmit(string text)
        {
            SendMessage();
        }

        private void OnSendClicked()
        {
            SendMessage();
        }

        private void SendMessage()
        {
            if (chatInput == null) return;

            string text = chatInput.text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            var gm = GameManager.Instance;
            if (gm == null || !gm.IsConnected) return;

            // Call the SendChat reducer
            gm.Connection.Reducers.SendChat(text);

            // Clear input
            chatInput.text = "";
            chatInput.ActivateInputField();
        }

        // -------------------------------------------------------
        // Scroll Control
        // -------------------------------------------------------

        /// <summary>
        /// Called by ScrollRect when the user scrolls manually.
        /// Disables auto-scroll if the user scrolls up, re-enables when at bottom.
        /// </summary>
        public void OnScrollValueChanged(Vector2 value)
        {
            _autoScroll = value.y <= 0.01f;
        }

        /// <summary>
        /// Force scroll to the bottom.
        /// </summary>
        public void ScrollToBottom()
        {
            _autoScroll = true;
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        /// <summary>
        /// Check if the chat input is currently focused (to suppress game input).
        /// </summary>
        public bool IsInputFocused => chatInput != null && chatInput.isFocused;
    }
}

#endif // STDB_BINDINGS
