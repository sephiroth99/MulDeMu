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
			uint srcCount = 0;
            bool isSeparatedChannels = false;
            bool isCineOutput = false;

			Console.Write("\nMulDeMu 0.1.1\nby sephiroth99\n\n");
			
			if (args.Length <= 0 || args[0] == "-h" || args[0] == "-?" || args[0] == "--help")
			{
                Console.Write("This program demultiplexes Tomb Raider Underworld mul files. By default, audio channels are written in a single WAV file.\n\n");   
                Console.Write("Usage:\nmuldemu [options] file1 [file2 ...]\n\n");
                Console.Write("Options:\n -s --split  Write each audio channel to its own WAV file\n -c --cine   Enable cine data output\n -h --help   This help menu\n");
				System.Environment.Exit(0);
			}
            else
            {
                int arg = 0;
                while(args[arg].StartsWith("-"))
                {
                    if (args[arg] == "-s" || args[arg] == "--split")
                    {
                        isSeparatedChannels = true;
                    }
                    else if (args[arg] == "-c" || args[arg] == "--cine")
                    {
                        isCineOutput = true;
                    }
                    arg++;
                }

                if(arg != 0)
                {
                    srcCount = (uint)(args.Length - arg);
                    args = args.Skip(arg).ToArray();
                }
                else
                {
                    srcCount = (uint)args.Length;
                }
            }

			for (int i = 0; i < srcCount; i++)
			{
				currPath = args[i];
				currFile = Path.GetFileName(currPath);

				Console.Write("[{1}/{2}] - {0}\n", currFile, i + 1, srcCount);

				try
				{
					MulFile file = new MulFile(new BinaryReader(File.Open(currPath, FileMode.Open, FileAccess.Read)));
					Console.WriteLine("Sampling rate      : {0} Hz\nNumber of channels : {1}\n", file.SamplingRate, file.NbChannelMul);

					if (file.IsCineData && isCineOutput)
					{
						MemoryStream cineData = file.readCineData();
						FileStream cineFile = new FileStream(String.Format("{0}.cine", currFile), FileMode.Create);
						cineFile.Write(cineData.ToArray(), 0, (int)cineData.Length);
						cineFile.Close();
						Console.WriteLine(String.Format("Cinematic data found and written to {0}.cine", currFile));
					}

                    if (isSeparatedChannels)
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
