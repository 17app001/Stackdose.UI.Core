namespace Stackdose.UI.Core.Controls
{
    public class DragPayload
    {
        public TabViewModel? Tab { get; set; }
        public DraggableTabContainer? Source { get; set; }
        public bool HandledByTarget { get; set; }
    }
}
