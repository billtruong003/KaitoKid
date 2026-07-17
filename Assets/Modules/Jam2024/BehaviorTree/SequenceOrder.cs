using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree
{
    /* 
     * SequenceOrder Nodes execute their children in order until one of them fails.
     * They are designed to ensure each child node runs in sequence and succeeds.
     * They stop executing when one of their children fails. 
     * If a SequenceOrder's child fails, the SequenceOrder fails. 
     * If all the SequenceOrder's children succeed, the SequenceOrder succeeds.
     */
    public class SequenceOrder : Node
    {
        private List<Node> SucceedNode = new List<Node>(); // List to track succeeded nodes

        // Default constructor calling the base class constructor
        public SequenceOrder() : base() { }

        // Constructor with a list of child nodes
        public SequenceOrder(List<Node> children) : base(children)
        {
            
        }

        /* 
         * Evalute method executes each child node in the sequence.
         * It checks the state of each child and decides the overall state of the sequence.
         */
        public override NodeState Evalute()
        {
            bool anyChildIsRunning = false; // Flag to check if any child is running

            // Iterate through each child node
            foreach (Node child in children)
            {   
                if(child.type == NoteType.INTERUPT)
                {
                    switch (child.InteruptionTask())
                    {
                        case NodeState.RESTART:
                            SucceedNode.Clear();
                            break;
                    }
                }

                // Check if the child node has not already succeeded
                if (!SucceedNode.Contains(child))
                {
                    // Switch based on the evaluation of the child node
                    switch (child.Evalute())
                    {
                        // If any child fails, the sequence fails
                        case NodeState.FAILURE:
                            state = NodeState.FAILURE;
                            return state; // Exit the function early with failure state

                        // If the child succeeds, add it to the succeeded nodes list
                        case NodeState.SUCCESS:
                            SucceedNode.Add(child);
                            continue; // Continue to the next child node

                        // If any child is running, set the sequence to running
                        case NodeState.RUNNING:
                            anyChildIsRunning = true;
                            state = NodeState.RUNNING;
                            return state;

                        // Default case sets the state to success
                        default:
                            state = NodeState.SUCCESS;
                            return state;
                    }
                }
            }

            // Set the final state based on if any child is running
            state = anyChildIsRunning ? NodeState.RUNNING : NodeState.SUCCESS;

            // Clear the list of succeeded nodes for the next evaluation
            SucceedNode.Clear();
            return state;
        }
    }
}
