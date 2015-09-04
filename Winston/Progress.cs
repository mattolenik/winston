using System;

namespace Winston
{
    public class Progress
    {
        public Action<int> Update { get; set; } = _ => { };

        public Action Completed { get; set; } = () => { };

        public int Row { get; set; }

        public string Name { get; set; }

        internal int? Last { get; set; }

        internal string ProgressPrefix { get; set; }
    }
}