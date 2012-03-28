using System;
using System.Linq;
using System.IO;

namespace MulDeMu
{
	class Program
	{
		static void Main(string[] args)
		{
			string currFile = "";
			string currPath = "";
			uint filesCount = 0;
            bool isSeparate = false;

			Console.Write("\nMulDeMu 0.1.0\nby sephiroth99\n\n");
			
			if (args.Length <= 0 || args[0] == "-h" || args[0] == "-?" || args[0] == "--help")
			{
				Console.Write("Usage:\nmuldemu [option] file1 [file2 ...]\n\n");
                Console.Write("This program demultiplexes Tomb Raider Underworld mul files. By default, audio channels are written in a single WAV file.\n\n");
                Console.Write("Options:\n -s --split  Write each audio channel to its own WAV file\n -h --help   This help menu\n");
				System.Environment.Exit(0);
			}
            else if (args[0] == "-s" || args[0] == "--split")
            {
                isSeparate = true;
                filesCount = (uint)args.Length - 1;
                args = args.Skip(1).ToArray();
            }
            else
            {
                filesCount = (uint)args.Length;
            }


			for (int i = 0; i < filesCount; i++)
			{
				currPath = args[i];
				currFile = Path.GetFileName(currPath);

				Console.Write("[{1}/{2}] - {0}\n", currFile, i + 1, filesCount);

				try
				{
					MulFile file = new MulFile(new BinaryReader(File.Open(currPath, FileMode.Open, FileAccess.Read)));
					Console.WriteLine("Sampling rate      : {0} Hz\nNumber of channels : {1}\nLooped playback    : {2}", file.SamplingRate, file.NbChannelMul, (file.IsLoop)?"Yes":"No");

					if (file.IsCineData)
					{
						MemoryStream cineData = file.readCineData();
						FileStream cineFile = new FileStream(String.Format("{0}.cine", currFile), FileMode.Create);
						cineFile.Write(cineData.ToArray(), 0, (int)cineData.Length);
						cineFile.Close();
						Console.WriteLine(String.Format("Cinematic data found and written to {0}.cine", currFile));
					}

                    if (isSeparate)
                    {
                        for (int ch = 0; ch < file.NbChannelMul; ch++)
                        {
                            MemoryStream chanData = file.getAudioStream(ch);

                            WavFile audioFile = new WavFile();
                            audioFile.SetWavFilePCM(1, file.SamplingRate, (ushort)16);
                            audioFile.WriteData(chanData);
                            audioFile.SaveFile(String.Format("{0}-ch{1}.wav", currFile, ch + 1));

                            Console.WriteLine(String.Format("Audio data decoded to {0}-ch{1}.wav", currFile, ch + 1));
                        }
                    }
                    else
                    {
                        MemoryStream[] chanData = file.getAudioStreams();
                        MemoryStream muxData = new MemoryStream();

                        foreach(MemoryStream m in chanData)
                        {
                            m.Position = 0;
                        }

                        for(int samp = 0; samp < chanData[0].Length/2; samp++)
                        {
                            for (int ch = 0; ch < file.NbChannelMul; ch++)
                            {
                                muxData.WriteByte((byte)chanData[ch].ReadByte());
                                muxData.WriteByte((byte)chanData[ch].ReadByte());
                            }
                        }

                        WavFile audioFile = new WavFile();
                        audioFile.SetWavFilePCM((ushort)file.NbChannelMul, file.SamplingRate, (ushort)16);
                        audioFile.WriteData(muxData);
                        audioFile.SaveFile(String.Format("{0}.wav", currFile));

                        Console.WriteLine(String.Format("Audio data decoded to {0}.wav", currFile));
                    }					
				}
				catch (Exception e)
				{
					Console.WriteLine("\n*********************\nCrash and burn!\n{0}\n*********************", e.Message);
				}
			}

            Console.WriteLine("\nAll Done!");
		}
	}
}
