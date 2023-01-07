using System.Collections.Generic;

namespace TaskGuidance.BackgroundProcessing.Actions
{
    public class ActionPriorityComparer : IComparer<ActionPriorityValues>
    {
        public int Compare(ActionPriorityValues x, ActionPriorityValues y)
        {
            return (int)y - (int)x;
        }
    }
}
