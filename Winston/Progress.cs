using System;

namespace Winston
{
    public class Progress
    {
        public Action<int> UpdateInstall { get; set; } = _ => { };

        public Action<int> UpdateDownload { get; set; } = _ => { };

        public Action CompletedDownload { get; set; } = () => { };

        public Action CompletedInstall { get; set; } = () => { };

        public int Row { get; set; }

        public string Name { get; set; }

        internal int? Last { get; set; }

        internal string ProgressPrefix { get; set; }
    }
}