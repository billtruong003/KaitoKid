using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Opencoding.LogHistory;
using UnityEngine;

namespace Opencoding.Console
{ 
    public enum SaveFileDataType
    {
        JSON,
        BINARY,
        TEXT
    }

    public class SaveFileData
    {
        public byte[] Data { get; set; }
        public string Name { get; set; }
        public SaveFileDataType DataType { get; set; }

        public SaveFileData(string name, byte[] data, SaveFileDataType dataType)
        {
            Name = name;
            Data = data;
            DataType = dataType;
        }

        public SaveFileData(string name, string data, SaveFileDataType dataType)
        {
            Name = name;
            Data = Encoding.UTF8.GetBytes(data);
            DataType = dataType;
        }


        public string GetMimeType()
        {
            switch (DataType)
            {
                case SaveFileDataType.TEXT:
                    return "text/plain";
                case SaveFileDataType.BINARY:
                    return "application/octet-stream";
                case SaveFileDataType.JSON:
                    return "application/json";
                default:
                    throw new InvalidOperationException("Unrecognised save file data type: " + DataType);
            }
        }
    }

    class LogHistoryEmailView
	{
		private readonly string _logEmailHeader;
		private readonly string _logEmailFooter;
		private readonly DebugConsole _debugConsole;

		public LogHistoryEmailView(string logEmailHeader, string logEmailFooter, DebugConsole debugConsole)
		{
			_logEmailHeader = logEmailHeader;
			_logEmailFooter = logEmailFooter;
			_debugConsole = debugConsole;
		}

		public void WriteToFile(Stream stream, byte[] screenshotData, IEnumerable<SaveFileData> saveFiles, IEnumerable<KeyValuePair<string, string>> gameInfo)
		{
			var streamWriter = new StreamWriter(stream);
			streamWriter.WriteLine(_logEmailHeader);

			var logHistoryInstance = LogHistory.LogHistory.Instance;
			var gameStartTime = logHistoryInstance.GameStartTime;
			int i = 0;
			foreach (var item in logHistoryInstance.LogItems)
			{
				++i;
				string categoryClass = "";
				if (item._Type == LogHistoryLogType.Warning)
					categoryClass = "header-warning";
				else if (item._Type == LogHistoryLogType.Error)
					categoryClass = "header-error";
				else if (item._Type == LogHistoryLogType.Exception)
					categoryClass = "header-exception";
				else if (item._Type == LogHistoryLogType.Assert)
					categoryClass = "header-assert";
				else if (item._Type == LogHistoryLogType.Log)
					categoryClass = "header-info";
				else if (item._Type == LogHistoryLogType.ConsoleInput)
					categoryClass = "header-input";

				var realTime = gameStartTime.AddSeconds(item._Time).ToString("G");
				streamWriter.WriteLine("<div class='log-row'><div class='header " + categoryClass + "'><span class='selected-marker'></span><div class='time' title='" + realTime + "'>" + item._Time +
				                 "</div><div class='header-text'>" + ToHtmlSafeString(item._LogMessage.Trim()) + "</div></div>");
				streamWriter.WriteLine("<div class='stacktrace'>" + ToHtmlSafeString(item._StackTrace) + "</div></div>");
			}

			var gameInfoString = "";
			if (gameInfo != null && gameInfo.Any())
			{
				gameInfoString = ToHTMLTable(gameInfo);
			}

		    var saveFileData = GenerateSaveFileBody(saveFiles);

		    var footer = _logEmailFooter
				.Replace("<!-- GENERAL INFORMATION -->", GenerateGeneralInformationSection())
				.Replace("<!-- HARDWARE INFORMATION -->", GenerateHardwareInformationSection())
				.Replace("<!-- SCREENSHOT -->", GenerateScreenshot(screenshotData))
				.Replace("<!-- SAVE FILE -->", saveFileData.ToString())
				.Replace("<!-- GAME INFORMATION -->", gameInfoString);

			streamWriter.WriteLine(footer);
			streamWriter.Close();
		}

        private static StringBuilder GenerateSaveFileBody(IEnumerable<SaveFileData> saveFiles)
        {
            string saveFileTemplate = @"<div class='panel panel-default' id='panel<!-- ID -->'>
  <div class='panel-heading'>
    <h4 class='panel-title'>
      <a data-toggle='collapse' data-target='#collapse<!-- ID -->' href='#collapse<!-- ID -->'><!-- TITLE --></a> <div style='float:right'><!-- DOWNLOAD_BUTTON --></div>
    </h4>
  </div>
  <div id='collapse<!-- ID -->' class='panel-collapse collapse in'>
      <div class='panel-body'><!-- BODY --></div>
  </div>
</div>";

            int i = 0;
            StringBuilder saveFileData = new StringBuilder();
            foreach (var saveFile in saveFiles)
            {
                var mimeType = saveFile.GetMimeType();

                string downloadButtonText = "";
                downloadButtonText = "<button class='btn btn-secondary btn-sm' onclick=\"downloadURI('data: " + mimeType + ";base64," + Convert.ToBase64String(saveFile.Data)
                                   + "', '" + saveFile.Name + "');\">Download</button>";
                string saveFileBody = "";
                switch (saveFile.DataType)
                {
                    case SaveFileDataType.JSON:
                        saveFileBody = "<pre class='json' id='data" + i + "'>" + Encoding.UTF8.GetString(saveFile.Data) + "</pre>";
                        ;
                        break;
                    case SaveFileDataType.BINARY:
                        break;
                    case SaveFileDataType.TEXT:
                        saveFileBody = "<pre id='data" + i + "'>" + Encoding.UTF8.GetString(saveFile.Data) + "</pre>";
                        break;
                    default:
                        throw new InvalidOperationException("Unrecognised save file data type " + saveFile.DataType);
                }

                saveFileData.Append(saveFileTemplate
                    .Replace("<!-- ID -->", i.ToString())
                    .Replace("<!-- TITLE -->", saveFile.Name)
                    .Replace("<!-- BODY -->", saveFileBody))
                    .Replace("<!-- DOWNLOAD_BUTTON -->", downloadButtonText);
                i++;
            }
            return saveFileData;
        }

        private string GenerateScreenshot(byte[] screenshotData)
		{
			if (screenshotData == null)
				return "";

			return "<img src='data:image/png;base64," + Convert.ToBase64String(screenshotData) + "'/>";
		}


		private string GenerateHardwareInformationSection()
		{
			var info = new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("Operating System", SystemInfo.operatingSystem),
				new KeyValuePair<string, string>("Processor Type", SystemInfo.processorType),
				new KeyValuePair<string, string>("Processor Count", SystemInfo.processorCount.ToString()),
				new KeyValuePair<string, string>("System Memory Size", SystemInfo.systemMemorySize.ToString()),
				new KeyValuePair<string, string>("Graphics Memory Size", SystemInfo.graphicsMemorySize.ToString()),
				new KeyValuePair<string, string>("Graphics Device Name", SystemInfo.graphicsDeviceName),
				new KeyValuePair<string, string>("Graphics Device Vendor", SystemInfo.graphicsDeviceVendor),
				new KeyValuePair<string, string>("Graphics Device ID", SystemInfo.graphicsDeviceID.ToString()),
				new KeyValuePair<string, string>("Graphics Device Vendor ID", SystemInfo.graphicsDeviceVendorID.ToString()),
				new KeyValuePair<string, string>("Graphics Device Version", SystemInfo.graphicsDeviceVersion),
				new KeyValuePair<string, string>("Graphics Shader Level", SystemInfo.graphicsShaderLevel.ToString()),
				new KeyValuePair<string, string>("Supports Shadows", SystemInfo.supportsShadows.ToString()),
				new KeyValuePair<string, string>("Supports 3D Textures", SystemInfo.supports3DTextures.ToString()),
				new KeyValuePair<string, string>("Supports Compute Shaders", SystemInfo.supportsComputeShaders.ToString()),
				new KeyValuePair<string, string>("Supports Instancing", SystemInfo.supportsInstancing.ToString()),
				new KeyValuePair<string, string>("Supports Sparse Textures", SystemInfo.supportsSparseTextures.ToString()),
				new KeyValuePair<string, string>("Supported Render Target Count", SystemInfo.supportedRenderTargetCount.ToString()),
				new KeyValuePair<string, string>("Npot Support", SystemInfo.npotSupport.ToString()),
				new KeyValuePair<string, string>("Device Name", SystemInfo.deviceName),
				new KeyValuePair<string, string>("Device Model", SystemInfo.deviceModel),
				new KeyValuePair<string, string>("Supports Accelerometer", SystemInfo.supportsAccelerometer.ToString()),
				new KeyValuePair<string, string>("Supports Gyroscope", SystemInfo.supportsGyroscope.ToString()),
				new KeyValuePair<string, string>("Supports Location Service", SystemInfo.supportsLocationService.ToString()),
				new KeyValuePair<string, string>("Supports Vibration", SystemInfo.supportsVibration.ToString()),
				new KeyValuePair<string, string>("Device Type", SystemInfo.deviceType.ToString()),
				new KeyValuePair<string, string>("Max Texture Size", SystemInfo.maxTextureSize.ToString())
			};

			return ToHTMLTable(info);
		}

		private string ToFriendlyName(string unfriendlyName)
		{
			string friendlyName = "";
			for (int i = 0; i < unfriendlyName.Length; ++i)
			{
				if (Char.IsUpper(unfriendlyName[i]))
				{
					friendlyName += " ";
				}

				if (friendlyName == "")
				{
					friendlyName += Char.ToUpper(unfriendlyName[i]);
				}
				else
				{
					friendlyName += Char.ToLower(unfriendlyName[i]);
				}
			}
			return friendlyName.Replace("i d", "ID").Replace("3 d", " 3D"); // dodgy hack
		}

		private string GenerateGeneralInformationSection()
		{
			var info = new List<KeyValuePair<string, string>>();
			info.Add(new KeyValuePair<string, string>("Game version", String.IsNullOrEmpty(_debugConsole.Settings.GameVersion) ? "&lt;not set&gt;" : _debugConsole.Settings.GameVersion));
			info.Add(new KeyValuePair<string, string>("Platform", Application.platform.ToString()));
			if(!String.IsNullOrEmpty(System.Environment.UserName))
				info.Add(new KeyValuePair<string, string>("Username",System.Environment.UserName));
			return ToHTMLTable(info);
		}

		private string ToHTMLTable(IEnumerable<KeyValuePair<string, string>> data)
		{
			var stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("<table class='table'><tbody>");
			foreach (var item in data)
			{
				stringBuilder.AppendLine("<tr><td>" + item.Key + "</td><td>" + item.Value + "</td></tr>");
			}
			stringBuilder.AppendLine("</tbody></table>");
			return stringBuilder.ToString();
		}

		private string ToHtmlSafeString(string input)
		{
			return input.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\n", "<br>");
		}
	}
}