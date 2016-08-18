using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ThingMagic;

namespace RfidEncoder
{
    public class ReaderMockup : Reader
    {
        public ReaderMockup()
        {
            ParamAdd(new Setting("/reader/baudRate", typeof (int), 0, true));
            ParamAdd(new Setting("/reader/gen2/Target", typeof (Gen2.Target), Gen2.Target.A, true));
            ParamAdd(new Setting("/reader/gen2/q", typeof (Gen2.Q), new Gen2.DynamicQ(), true));
            ParamAdd(new Setting("/reader/region/supportedRegions", typeof (Region[]), new []{Region.AU, Region.EU, Region.NA, Region.JP}, true));
            ParamAdd(new Setting("/reader/region/id", typeof (Region), Region.UNSPEC, true));
            ParamAdd(new Setting("/reader/radio/readPower", typeof (int), 100, true));
            ParamAdd(new Setting("/reader/tagop/antenna", typeof (int), 1, true));
            ParamAdd(new Setting("/reader/tagop/protocol", typeof (TagProtocol), TagProtocol.NONE, true));
            ParamAdd(new Setting("/reader/read/plan", typeof (ReadPlan), null, true));
            //ParamSet("/reader/baudRate", 0);
        }

        public override void Connect()
        {
            Thread.Sleep(500);
            MessageBox.Show("Reader connected");
        }

        public override void ReceiveAutonomousReading()
        {
            throw new NotImplementedException();
        }

        public override void Reboot()
        {
            throw new NotImplementedException();
        }

        public override void Destroy()
        {
            Thread.Sleep(300);
            MessageBox.Show("Reader destroyed");
        }

        public override TagReadData[] Read(int timeout)
        {
            throw new NotImplementedException();
        }

        byte[] _previousTag;
        volatile bool _isReading;
        public override void StartReading()
        {
            if(_isReading) return;
            lock (this)
            {
                if (_isReading) return;
                var rnd = new Random();
                _isReading = true;
                Task.Factory.StartNew(() =>
                {

                    Thread.Sleep(rnd.Next(1000, 5000));
                    //if (!_isReading) break;

                    var data = new TagReadData();
                    var epcField = typeof (TagReadData).GetField("_tagData",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    epcField.SetValue(data, new TagData(_previousTag ??
                                                        new[]
                                                        {
                                                            (byte) rnd.Next(256), (byte) rnd.Next(256),
                                                            (byte) rnd.Next(256), (byte) rnd.Next(256)
                                                        }));
                    OnTagRead(data);
                    _previousTag = null;
                });
            }
        }

        public override void StopReading()
        {
            lock (this)
            {
                _isReading = false;
            }
        }

        public override void FirmwareLoad(Stream firmware)
        {
            throw new NotImplementedException();
        }

        public override void FirmwareLoad(Stream firmware, FirmwareLoadOptions flOptions)
        {
            throw new NotImplementedException();
        }

        public override GpioPin[] GpiGet()
        {
            throw new NotImplementedException();
        }

        public override void GpoSet(ICollection<GpioPin> state)
        {
            throw new NotImplementedException();
        }

        public override object ExecuteTagOp(TagOp tagOP, TagFilter target)
        {
            if (tagOP is Gen2.WriteTag)
            {
                Thread.Sleep(300);
                _previousTag = ((Gen2.WriteTag)tagOP).Epc.EpcBytes;
                MessageBox.Show("Tag " + ((Gen2.WriteTag)tagOP).Epc + " has been written");
            }
            return null;
        }

        public override void KillTag(TagFilter target, TagAuthentication password)
        {
            throw new NotImplementedException();
        }

        public override void LockTag(TagFilter target, TagLockAction action)
        {
            throw new NotImplementedException();
        }

        public override byte[] ReadTagMemBytes(TagFilter target, int bank, int byteAddress, int byteCount)
        {
            throw new NotImplementedException();
        }

        public override ushort[] ReadTagMemWords(TagFilter target, int bank, int wordAddress, int wordCount)
        {
            throw new NotImplementedException();
        }

        public override void WriteTag(TagFilter target, TagData epc)
        {
            Thread.Sleep(300);
            _previousTag = epc.EpcBytes;
            MessageBox.Show("Tag written");
        }

        public override void WriteTagMemBytes(TagFilter target, int bank, int address, ICollection<byte> data)
        {
            throw new NotImplementedException();
        }

        public override void WriteTagMemWords(TagFilter target, int bank, int address, ICollection<ushort> data)
        {
            throw new NotImplementedException();
        }
    }
}
