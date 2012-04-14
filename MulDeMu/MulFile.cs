using System;
using System.IO;

namespace MulDeMu
{
    static class Constants
    {
        public const uint CHUNK_TYPE_CINE = 0x01;
        public const uint CHUNK_TYPE_AUDIO = 0x00;
        public const uint FIRST_CHUNK_OFFSET = 0x800;
        public const uint MAGIC_CINE_CHUNK = 0x43494e45;
        public const uint SIZE_CHUNK_HEADER = 16;
    }

	public class MulFile
	{
		private UInt32 samplingRate;
		private UInt32 loopAdr;
		private UInt32 sampleCount;
		//private UInt32 audioChunkCount;
		private UInt32 nbChannelMul;

		private bool isValidMulFile;
		private bool isCineData;
		private bool isLoop;

		private BinaryReader data;

        private MemoryStream[] audioStreams;
        private bool isAudioDecoded = false;

		internal MulFile(String path)
		{
			this.data = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read));
			this.parseMulStream(this.data);

			if(this.isValidMulFile == false)
			{
				throw new MulFileException("Invalid file or wrong MUL format!");
			}
		}
		
		internal MulFile(BinaryReader rdr)
		{
			this.data = rdr;
			this.parseMulStream(this.data);

			if (this.isValidMulFile == false)
			{
				throw new MulFileException("Invalid file or wrong MUL format!");
			}
		}

		private void parseMulStream(BinaryReader data)
		{
			UInt32 type;
			UInt32 audioChunks = 0;
			UInt32 cineCounter = 0;
			UInt32 nextChunkAdr = 0;

			// Read MUL header
			data.BaseStream.Seek(0, SeekOrigin.Begin);

			this.samplingRate = data.ReadUInt32();
			this.loopAdr = data.ReadUInt32();
			if (this.loopAdr == UInt32.MaxValue)
			{
				this.isLoop = false;
			}
			else
			{
				this.isLoop = true;
			}

			this.sampleCount = data.ReadUInt32();
			this.nbChannelMul = data.ReadUInt32();

			// Read chunk count
			// Offset valid for TRU MUL files only (?)
			//data.BaseStream.Seek(40, SeekOrigin.Current);			
			//this.audioChunkCount = (UInt32)data.ReadSingle();
			
			// Header done

			// Set cursor at start of data
			nextChunkAdr = Constants.FIRST_CHUNK_OFFSET;
			data.BaseStream.Seek(nextChunkAdr, SeekOrigin.Begin);
			
			// Read first chunk, may be special cinematic chunk
			type = data.ReadUInt32();
			if (type == Constants.CHUNK_TYPE_AUDIO)
			{
				// First chunk is not of type cine
				this.isCineData = false;
				audioChunks++;

				// Get next chunk adr
				nextChunkAdr += (data.ReadUInt32() + 16);

				// Goto next chunk
				data.BaseStream.Seek(nextChunkAdr, SeekOrigin.Begin);
			}
			else if (type == Constants.CHUNK_TYPE_CINE)
			{
				// Get next chunk address
				nextChunkAdr += (data.ReadUInt32() + 16);

				// Seek to start of cine chunk
				data.BaseStream.Seek(8, SeekOrigin.Current);

				// Check magic value
				UInt32 magic = data.ReadUInt32();
				if (magic != Constants.MAGIC_CINE_CHUNK)
				{
					this.isCineData = false;
					throw new MulFileException("Wrong cinematic data magic value!");
				}
				else
				{
					this.isCineData = true;
					cineCounter++;
				}

				// Goto next chunk
				data.BaseStream.Seek(nextChunkAdr, SeekOrigin.Begin);
			}
			else
			{
				throw new MulFileException(string.Format("Error chunk type at offset 0x{0:X}!", data.BaseStream.Position));
			}

			while (data.BaseStream.Position < data.BaseStream.Length)
			{
				type = data.ReadUInt32();
				if (type == Constants.CHUNK_TYPE_AUDIO)
				{
					// Get next chunk adr
					nextChunkAdr += (data.ReadUInt32() + 16);

					audioChunks++;
				}
				else if (type == Constants.CHUNK_TYPE_CINE)
				{
					UInt32 cineSeq;
					UInt32 cineSize;

					nextChunkAdr += (data.ReadUInt32() + 16);
					
					//Start of cine chunk
					data.BaseStream.Seek(8, SeekOrigin.Current);

					//Read cine size
					cineSize = data.ReadUInt32();
					//Check cine size validity
					if ((cineSize + data.BaseStream.Position) != (nextChunkAdr))
					{
						throw new MulFileException("Bad cine size!");
					}
					
					//Read cine seq
					cineSeq = data.ReadUInt32();
					//Check cine seq validity
					if (cineSeq != cineCounter)
					{
						if (cineSeq != UInt32.MaxValue)
						{
							throw new MulFileException(string.Format("Bad cine sequence {0}, expected {1}!", cineSeq, cineCounter));
						}
					}
					else
					{
						cineCounter++;
					}
				}
				else
				{
					throw new MulFileException(string.Format("Error chunk type at offset 0x{0:X}!", data.BaseStream.Position));
				}

				data.BaseStream.Seek(nextChunkAdr, SeekOrigin.Begin);
			}

			/*if (audioChunks == (this.audioChunkCount + 1))
			{
				this.isValidMulFile = true;
			}
			else
			{
				throw new MulFileException("Wrong chunk count!");
			}*/
            this.isValidMulFile = true;
			
		}

		public MemoryStream readCineData()
		{
			UInt32 nextChunkAdr;
			UInt32 type;
			UInt32 chunkDataSize;

			if (this.isCineData == false)
			{
				return null;
			}
			
			MemoryStream ms = new MemoryStream();
			BinaryWriter output = new BinaryWriter(ms);
			
			// Write cine chunks
			// (keep 16 bytes alignment)
			
			// Seek to first chunk
			nextChunkAdr = Constants.FIRST_CHUNK_OFFSET;
			this.data.BaseStream.Seek(nextChunkAdr, SeekOrigin.Begin);
						
			while (data.BaseStream.Position < data.BaseStream.Length)
			{
				type = this.data.ReadUInt32();
				chunkDataSize = this.data.ReadUInt32();
				nextChunkAdr += (chunkDataSize + 16);
				if (type == Constants.CHUNK_TYPE_CINE)
				{
					// Seek start of chunk data
					this.data.BaseStream.Seek(8, SeekOrigin.Current);

					output.Write(this.data.ReadBytes((int)chunkDataSize));
				}

				this.data.BaseStream.Seek(nextChunkAdr, SeekOrigin.Begin);
			}

			return ms;
		}

        public MemoryStream[] getAudioStreams()
        {
            if (isAudioDecoded == false)
            {
                decodeAudio();
            }

            return audioStreams;
        }
        
        public MemoryStream getAudioStream(int channel)
        {
            if (channel >= this.nbChannelMul)
            {
                throw new MulFileException("Invalid channel!");
            }
            
            if (isAudioDecoded == false)
            {
                decodeAudio();
            }

            return audioStreams[channel];
        }

		public void decodeAudio()
		{
			UInt32 nextChunkAdr;
			UInt32 type;
			UInt32 audioDataSize;

            audioStreams = new MemoryStream[this.nbChannelMul];
            for (int i = 0; i < this.nbChannelMul; i++)
            {
                audioStreams[i] = new MemoryStream();
            }

			// Seek to first chunk
			nextChunkAdr = Constants.FIRST_CHUNK_OFFSET;
			this.data.BaseStream.Seek(nextChunkAdr, SeekOrigin.Begin);

			while (data.BaseStream.Position < data.BaseStream.Length)
			{
				type = this.data.ReadUInt32();
				nextChunkAdr += (this.data.ReadUInt32() + 16);

                // Seek start of audio chunk header
                this.data.BaseStream.Seek(8, SeekOrigin.Current);

				if (type == Constants.CHUNK_TYPE_AUDIO)
				{
					//Read audio data size
					audioDataSize = this.data.ReadUInt32();

					// Seek start of audio data
					this.data.BaseStream.Seek(12, SeekOrigin.Current);

					// Must be multiple of 36 bytes
					if ((audioDataSize % 36) != 0)
					{
						throw new MulFileException("Chunk not multiple of 36!");
					}

                    // Separate chunk in part for the channels
                    for (int ch = 0; ch < this.nbChannelMul; ch++)
                    {
                        var blocks = (audioDataSize / this.nbChannelMul) / 36;

                        for (uint bl = 0; bl < blocks; bl++)
                        {
                            var dec = CDADPCM.DecodeBlock(this.data.ReadBytes(36));
                            audioStreams[ch].Write(dec, 0, dec.Length);
                        }
                    }
				}

				this.data.BaseStream.Seek(nextChunkAdr, SeekOrigin.Begin);
			}

            this.isAudioDecoded = true;
		}
        
		public class MulFileException : Exception
		{
			public MulFileException()
			{
			}
			public MulFileException(string message) : base(message)
			{
			}
			public MulFileException(string message, Exception inner) : base(message, inner)
			{
			}
		}

		internal UInt32 SamplingRate
		{
			get
			{
				return this.samplingRate;
			}
		}

		internal UInt32 LoopAdr
		{
			get
			{
				return this.loopAdr;
			}
		}

		internal UInt32 SampleCount
		{
			get
			{
				return this.sampleCount;
			}
		}

		/*internal UInt32 AudioChunkCount
		{
			get
			{
				return this.audioChunkCount;
			}
		}*/

		internal UInt32 NbChannelMul
		{
			get
			{
				return this.nbChannelMul;
			}
		}

		internal bool IsCineData
		{
			get
			{
				return this.isCineData;
			}
		}

		internal bool IsLoop
		{
			get
			{
				return this.isLoop;
			}
		}

		internal bool IsValidMulFile
		{
			get
			{
				return this.isValidMulFile;
			}
		}
	}
}
