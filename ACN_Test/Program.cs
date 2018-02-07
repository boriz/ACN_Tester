using E131;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;

namespace ACN_Test
{
    class Program
    {
        static Guid guid = new Guid();
        static byte seq;
        static UdpClient udp;

        static void Main(string[] args)
        {
            Console.WriteLine("E1.31 test sender. !!! All parameters are hardcoded !!!");

            // Assuming 150 pixles RGB channels
            byte[] d = new byte[150 * 3];

            // Create UPD conenction
            try
            {
                udp = new UdpClient("192.168.222.7", 5568);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Can't open, Exception: " + ex.ToString());
                Environment.Exit(0);
            }

            // Cycling through few colors
            int color_code = 0;

            // Main loop
            Console.WriteLine("Press ESC to stop");
            do
            {
                while (!Console.KeyAvailable)
                {                         
                    // Update colors (R-G-B-Off)
                    for (int i=0; i<(d.Length-3); i+=3)
                    {
                        // R
                        d[i] = (color_code == 0) ? (byte)255 : (byte)0;

                        // G
                        d[i+1] = (color_code == 1) ? (byte)255 : (byte)0;

                        // B
                        d[i+2] = (color_code == 2) ? (byte)255 : (byte)0;
                    }
                    color_code = (color_code >= 3) ? 0 : color_code + 1;

                    // Send the same packet to multiple universes
                    for (UInt16 i = 1; i <= 9; i++)
                    {
                        SendPacket(i, d);

                        // My controller is pretty slow, give it some time to handle the packet
                        Thread.Sleep(50);   
                    }

                    // Delay
                    Thread.Sleep(1000);
                    Console.WriteLine();
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);

            // Clean up UDP connection
            udp.Close();
        }


        static private void SendPacket(UInt16 universe, byte[] d)
        {
            // Create packet
            E131Pkt pkt = new E131Pkt(guid, "Awesome Sender", seq, universe, d, 0, d.Length);

            // Send over UDP
            try
            {
                udp.Send(pkt.PhyBuffer, pkt.PhyLength);
                Console.WriteLine("Packet sent. Uni: "+universe.ToString() + "; Len: " + pkt.PhyLength+"; Seq: " + seq.ToString());
            }
            catch(Exception ex)
            {
                Console.WriteLine("Can't send, Exception: " + ex.ToString());
                Environment.Exit(0);
            }

            // Update sequence number
            seq++;
        }
    }
}
