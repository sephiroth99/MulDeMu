using System;
using System.Text;
using System.IO;

namespace MulDeMu
{
    enum WaveFormatTags
    {
        WAVE_FORMAT_PCM = 0x0001
    }

    class WavFile
    {
        private static byte[] containerIdentifier = Encoding.ASCII.GetBytes("RIFF");
        private static byte[] riffChunkIdentifier = Encoding.ASCII.GetBytes("WAVE");
        private static byte[] waveChunkIdentifierFormat = Encoding.ASCII.GetBytes("fmt ");
        private static byte[] waveChunkIdentifierData = Encoding.ASCII.GetBytes("data");

        private MemoryStream wavefile;
        private WaveFormatTags encoding;

        public WavFile() { }

        public void SetWavFilePCM(UInt16 chans, UInt32 sampRate, UInt16 bitsPerSamp)
        {
            this.wavefile = new MemoryStream();
            BinaryWriter output = new BinaryWriter(this.wavefile);

            this.encoding = WaveFormatTags.WAVE_FORMAT_PCM;

            // Write fourcc
            output.Write(containerIdentifier);

            // Skip length
            output.BaseStream.Seek(4, SeekOrigin.Current);

            // Write riff chunk id
            output.Write(riffChunkIdentifier);

            // Start of wave format chunk "fmt "
            output.Write(waveChunkIdentifierFormat);

            // Length of format chunk
            output.Write((uint)16);

            // Format tag
            output.Write((ushort)this.encoding);

            // nb channels
            output.Write((ushort)chans);

            // sampling rate
            output.Write((uint)sampRate);

            // average bytes per sec
            output.Write((uint)(sampRate * chans * bitsPerSamp / 8));

            // block size of data in bytes
            output.Write((ushort)(chans * bitsPerSamp / 8));

            // Number of bits per sample of mono data
            output.Write((ushort)bitsPerSamp);

            // Start of wave data chunk
            output.Write(waveChunkIdentifierData);

            // Ready for data!
        }

        public void WriteData(byte[] data)
        {
            BinaryWriter w = new BinaryWriter(this.wavefile);

            // Write data length
            w.Write((uint)data.Length);

            // Write data
            w.Write(data, 0, (int)data.Length);

            // Write RIFF length
            w.BaseStream.Seek(4, SeekOrigin.Begin);
            w.Write((uint)w.BaseStream.Length - 8);
        }

        public void WriteData(MemoryStream data)
        {
            this.WriteData(data.ToArray());
        }

        public void SaveFile(String path)
        {
            this.wavefile.Position = 0;

            FileStream file = new FileStream(path, FileMode.Create);
            file.Write(this.wavefile.ToArray(), 0, (int)this.wavefile.Length);
            file.Close();
        }
    }
}
