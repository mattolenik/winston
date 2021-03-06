﻿using System;
using System.IO;
using FluentAssertions;
using Xunit;
using Winston.User;

namespace Winston.Test.User
{
    public class ConsoleUserAdapterTests : IDisposable
    {
        readonly StringWriter writer;
        readonly ConsoleUserAdapter adapter;

        public ConsoleUserAdapterTests()
        {
            writer = new StringWriter();
            var reader = new StringReader("ans1");
            adapter = new ConsoleUserAdapter(writer, reader);
        }

        [Fact]
        public async void AskAndAnswer()
        {
            var answer = await adapter.AskAsync(new Question("", "Question", "ans1", "ans2"));
            answer.Should().Be("ans1");
        }

        [Fact]
        public void ReceiveMessage()
        {
            adapter.Message("msg");
            var output = writer.GetStringBuilder().ToString();
            output.Should().Contain("msg");
        }

        public void Dispose()
        {
            writer.Dispose();
        }
    }
}
