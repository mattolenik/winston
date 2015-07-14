﻿using System;
using System.IO;

namespace Winston
{
    public class TempFile : IDisposable
    {
        public string Path { get; } = System.IO.Path.GetTempFileName();

        public void Dispose() => File.Delete(Path);

        public static implicit operator string(TempFile value) => value?.Path;
    }
}