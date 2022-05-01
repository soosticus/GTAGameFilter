using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using FastGithub.WinDiverts;
using FastGithub.WinDiverts.WinAPI;

namespace GTAGameFilter
{
    internal class Packet
    {
        public string SrcAddress { get; private set; }
        public string DestAddress { get; private set; }
        public string Payload { get; private set; }
        public bool IsAllowed { get; set; }
        public Packet(string src, string dst, string payload)
        {
            SrcAddress = src;
            DestAddress = dst;
            Payload = payload;
            IsAllowed = false;
        }

    }
    internal class PacketRecievedEventArgs : EventArgs
    {
        public Packet Packet { get; private set; }

        public PacketRecievedEventArgs(Packet packet)
        {
            Packet = packet;
        }
    }
    internal class PacketSniffer
    {
        string filter = "(udp.SrcPort == 6672 or udp.DstPort == 6672) and ip";
        IntPtr handle;
        Thread thread;

        public delegate void PacketHandler(object sender, PacketRecievedEventArgs e);
        public event PacketHandler? OnPacketRecieved;

        public PacketSniffer()
        {
            thread = new Thread(new ThreadStart(ThreadProc));
        }

        private bool HandlePacket(Packet packet)
        {
            // Make sure someone is listening to event
            if (OnPacketRecieved == null) return true;

            var args = new PacketRecievedEventArgs(packet);
            OnPacketRecieved(this, args);
            return packet.IsAllowed;
        }

        ~PacketSniffer()
        {
            Stop();
        }

        public void Start()
        {
            handle = WinDivert.WinDivertOpen(filter, WinDivertLayer.Network, 0, WinDivertOpenFlags.Sniff);
            if (handle == IntPtr.Zero)
            {
                throw new Exception(String.Format("Failed to start IPScanner, error: %d.", Marshal.GetLastWin32Error()));
            }
            thread.Start();
        }

        public void Stop()
        {
            thread.Join();
            WinDivert.WinDivertClose(handle);
        }
        bool s_running = true;
        public ushort SwapBytes(ushort x)
        {
            return (ushort)((ushort)((x & 0xff) << 8) | ((x >> 8) & 0xff));
        }

        private void ThreadProc()
        {
            var packet = new WinDivertBuffer();

            var addr = new WinDivertAddress();

            uint readLen = 0;

            Span<byte> packetData = null;

            NativeOverlapped recvOverlapped;

            IntPtr recvEvent = IntPtr.Zero;
            uint recvAsyncIoLen = 0;

            do
            {
                if (s_running)
                {
                    packetData = null;

                    readLen = 0;

                    recvAsyncIoLen = 0;
                    recvOverlapped = new NativeOverlapped();

                    recvEvent = Kernel32.CreateEvent(IntPtr.Zero, false, false, IntPtr.Zero);

                    if (recvEvent == IntPtr.Zero)
                    {
                        Debug.WriteLine("Failed to initialize receive IO event.");
                        continue;
                    }

                    addr.Reset();

                    recvOverlapped.EventHandle = recvEvent;

                    if (!WinDivert.WinDivertRecvEx(handle, packet, 0, ref addr, ref readLen, ref recvOverlapped))
                    {
                        var error = Marshal.GetLastWin32Error();

                        // 997 == ERROR_IO_PENDING
                        if (error != 997)
                        {
                            Kernel32.CloseHandle(recvEvent);
                            continue;
                        }

                        while (Kernel32.WaitForSingleObject(recvEvent, 1000) == (uint)WaitForSingleObjectResult.WaitTimeout)
                            ;

                        if (!Kernel32.GetOverlappedResult(handle, ref recvOverlapped, ref recvAsyncIoLen, false))
                        {
                            Debug.WriteLine("Failed to get overlapped result.");
                            Kernel32.CloseHandle(recvEvent);
                            continue;
                        }

                        readLen = recvAsyncIoLen;
                    }

                    Kernel32.CloseHandle(recvEvent);

                    Debug.WriteLine(String.Format("Read packet {0}", readLen));

                    WinDivertParseResult result = WinDivert.WinDivertHelperParsePacket(packet, readLen);

                    if (addr.Direction == WinDivertDirection.Inbound)
                    {
                        Debug.WriteLine("inbound!");
                    }
                    string srcAddr = "";
                    string dstAddr = "";
                    string payload = "";
                    unsafe
                    {
                        if (result.IPv4Header != null)
                        {
                            Debug.WriteLine($"V4 TCP packet {addr.Direction} from {result.IPv4Header->SrcAddr} to {result.IPv4Header->DstAddr}");
                            srcAddr = result.IPv4Header->SrcAddr.ToString();
                            dstAddr = result.IPv4Header->DstAddr.ToString();
                            //payload = System.Text.Encoding.Default.GetString(result.PacketPayload, (int) result.PacketPayloadLength);
                        }
                    }

                    if (packetData != null)
                    {
                        Debug.WriteLine(String.Format("Packet has {0} byte payload.", packetData.Length));
                    }

                    /*Debug.WriteLine($"{nameof(addr.Direction)} - {addr.Direction}");
                    Debug.WriteLine($"{nameof(addr.Impostor)} - {addr.Impostor}");
                    Debug.WriteLine($"{nameof(addr.Loopback)} - {addr.Loopback}");
                    Debug.WriteLine($"{nameof(addr.IfIdx)} - {addr.IfIdx}");
                    Debug.WriteLine($"{nameof(addr.SubIfIdx)} - {addr.SubIfIdx}");
                    Debug.WriteLine($"{nameof(addr.Timestamp)} - {addr.Timestamp}");
                    Debug.WriteLine($"{nameof(addr.PseudoIPChecksum)} - {addr.PseudoIPChecksum}");
                    Debug.WriteLine($"{nameof(addr.PseudoTCPChecksum)} - {addr.PseudoTCPChecksum}");
                    Debug.WriteLine($"{nameof(addr.PseudoUDPChecksum)} - {addr.PseudoUDPChecksum}");*/

                    // Debug.WriteLine(WinDivert.WinDivertHelperCalcChecksums(packet, ref addr, WinDivertChecksumHelperParam.All));

                    var p = new Packet(srcAddr, dstAddr, payload);
                    if (HandlePacket(p) && !WinDivert.WinDivertSendEx(handle, packet, readLen, 0, ref addr))
                    {
                        Debug.WriteLine(String.Format("Write Err: {0}", Marshal.GetLastWin32Error()));
                    }
                }
            }
            while (s_running);
        }
    }
}