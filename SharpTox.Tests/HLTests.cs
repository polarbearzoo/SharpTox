﻿using System;
using NUnit.Framework;
using SharpTox.HL;
using SharpTox.HL.Transfers;
using SharpTox.Core;
using System.IO;
using System.Threading;

namespace SharpTox.Tests
{
    [TestFixture]
    public class HLTests
    {
        [Test]
        public void HLTest()
        {
            bool finished = false;
            byte[] data = new byte[1 << 26];
            byte[] receivedData = new byte[1 << 26];
            new Random().NextBytes(data);

            var tox1 = new ToxHL(ToxOptions.Default);
            var tox2 = new ToxHL(ToxOptions.Default);

            tox1.Start();
            tox2.Start();

            tox1.AddFriend(tox2.Id, "test");
            tox2.FriendRequestReceived += (sender, args) =>
            {
                var friend = tox2.AddFriendNoRequest(args.PublicKey);
                friend.TransferRequestReceived += (s, e) => e.Transfer.Accept(new MemoryStream(receivedData));
            };

            while (!tox1.Friends[0].IsOnline)
            {
                Thread.Sleep(100);
            }

            var transfer = tox1.Friends[0].SendFile(new MemoryStream(data), "test.dat", ToxFileKind.Data);
            
            Console.WriteLine(transfer.Progress.ToString("P"));
            transfer.ProgressChanged += (sender, args) =>
            {
                Console.WriteLine(args.Progress.ToString("P"));
            };
            
            Console.WriteLine((transfer.Speed / 1000).ToString("F") + " kByte/sec");
            transfer.SpeedChanged += (sender, args) =>
            {
                Console.WriteLine((args.Speed / 1000).ToString("F") + " kByte/sec");
            };
            
            Console.WriteLine(transfer.ElapsedTime.ToString("HH:mm:ss"));
            transfer.ElapsedTimeChanged += (sender, args) =>
            {
                Console.WriteLine(args.Time.ToString("HH:mm:ss"));
            };

            transfer.StateChanged += (sender, e) =>
            {
                if (e.State == ToxTransferState.Finished)
                    finished = true;
                else if (e.State == ToxTransferState.Canceled)
                    Assert.Fail();
            };
            transfer.Errored += (sender, e) => Assert.Fail();

            while (!finished)
            {
                Thread.Sleep(100);
            }

            Console.WriteLine(transfer.ElapsedTime.ToString("HH:mm:ss"));

            tox1.Dispose();
            tox2.Dispose();
        }
    }
}
