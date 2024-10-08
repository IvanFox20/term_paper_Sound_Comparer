using System;
using System.IO;
using System.ComponentModel;
using System.Windows.Forms;
using SoundComparer.DigitalFilter;
using System.Collections.Generic;

namespace SoundComparer.WaveUtils
{
    /// <summary>Stores format and samples of a wave sound.</summary>
	public class WaveSound
    {
        #region Members

        private WaveFormat format; // ������ �����
        private short[] samples; // ������ ������� �����
        short[] a = new short[256]; // ������ ��� �������������� 8-������ ������� � 16-������
        private static int fftPoint = 512; // ���������� ����� ��� FFT
        private int framelen = 512; // ����� ������ ����� (� �������)
        private float[] A; // ��������� (� ���������), ������ ������ ������ 512 ��������� (1 ������� ~232.2 ��)
        private string filename = ""; // ��� ����� WAV
        private int nbframe = 0; // ���������� ������ ��� �������
        private FIRFilters firFilter; // ������ FIR-�������
        private int comparedFrame = 0; // ������� ���������� ������
        private string missData = ""; // ���������� ��� �������� ����������� ������
        private const int MAX_PROGRESS = 500; // ������������ �������� ��� ���������� ���������

        #endregion // Members


        #region Properties

        /// <summary>���������� ���������� ��������� �������.</summary>
        public WaveFormat Format
        {
            get { return format; }
        }

        /// <summary>���������� ���������������� ������� ���������� ��� ������������.</summary>
        public WaveControl GraphicControl
        {
            get;
            set;
        }

        /// <summary>���������� ������� ���������� ����������� ���������.</summary>
        public ProgressBar ProgressBar
        {
            get;
            set;
        }

        /// <summary>���������� ���������� �������.</summary>
        public int Count
        {
            get { return samples.Length; }
        }

        /// <summary>���������� ���������.</summary>
        public float[] Amplitude
        {
            get { return A; }
        }

        /// <summary>���������� ����� �� �������� �������.</summary>
        public short this[int indexer]
        {
            get { return samples[indexer]; }
            set { samples[indexer] = value; }
        }

        /// <summary>���������� ������ ������� �����.</summary>
        public short[] Samples
        {
            get { return samples; }
        }

        /// <summary>���������� ���������� ������ ��� ������� � �����������.</summary>
        public int NbFrame
        {
            get { return nbframe; }
        }

        /// <summary>���������� ��� ������ ��� �����.</summary>
        public string Filename
        {
            get { return filename; }
            set { filename = value; }
        }

        /// <summary>������ ���������� ����� FFT.</summary>
        public int FFTPoint
        {
            set { fftPoint = value; }
        }

        /// <summary>������ ����� �����.</summary>
        public int Frame
        {
            set { framelen = value; }
        }

        #endregion // Properties


        #region Constructors

        /// <summary>�����������.</summary>
        /// <param name="format">���������� ��������� �������.</param>
        /// <param name="samples">������ ����� WAV.</param>
        public WaveSound(WaveFormat format, short[] samples)
        {
            // ���������� ������� 'a' ���������� �� -128 �� 127
            for (short i = -128; i < 0; i++)
                a[128 + i] = i;
            for (short i = 0; i < 128; i++)
                a[128 + i] = i;

            firFilter = new FIRFilters(); // ������������� FIR-�������
            this.format = format; // ��������� �������
            this.samples = samples; // ��������� �������
        }

        /// <summary>�����������.</summary>
        /// <param name="fileName">���� � WAV �����.</param>
        /// <param name="graphicControl">����������� ������, �� ������� ����� ������������ ������ WAV.</param>
        /// <param name="progressBar">��������� ��������� ��� ����������� ���� �������� � ������ WAV �����.</param>
        public WaveSound(string fileName, WaveControl graphicControl, ProgressBar progressBar)
        {
            // ������������� �������� ����������������� ���������� (����������� ������� � ��������� ���������)
            GraphicControl = graphicControl; // ��������� ������������ �����������
            ProgressBar = progressBar; // ��������� ���������� ���������
            ProgressBar.Maximum = MAX_PROGRESS; // ��������� ������������� �������� ���������� ���������
            ProgressBar.Value = 0; // ��������� �������� ���������� ���������
            ProgressBar.Step = 1; // ��� ���������� ���������

            // ���������� ������� 'a' ���������� �� -128 �� 127
            for (short i = -128; i < 0; i++)
                a[128 + i] = i;
            for (short i = 0; i < 128; i++)
                a[128 + i] = i;

            firFilter = new FIRFilters(); // ������������� FIR-�������
            filename = fileName; // ��������� ����� �����
            samples = new short[4096]; // ������������� ������� ������� �������� 4096
        }

        #endregion // Constructors


        #region Functions to process WAV

        /// <summary>������ WAV-���� � �������������� �������� �������� ��������, ��������� ���������� �������� � ���������.</summary>
        /// <returns></returns>

        public void ReadWavFile()
        {
            using (var bgw = new BackgroundWorker())
            {
                bgw.ProgressChanged += bgw_ProgressChanged;
                bgw.DoWork += bgw_DoWork;
                bgw.RunWorkerCompleted += bgw_WorkCompleted;
                bgw.WorkerReportsProgress = true;
                bgw.RunWorkerAsync();
            }
        }

        /// <summary>������ WAV-���� � ���������� �������� � ���������.</summary>
        /// <param name="sender">������� ����������, ��� �������� ����������� ��������</param> 
        /// <param name="e">��������� �������</param>
        /// <returns></returns>

        public void bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            // ������ WAV-����� � ������ FFT-�����
            using (BinaryReader reader = new BinaryReader(new FileStream(filename, FileMode.Open)))
            using (BinaryWriter fft = new BinaryWriter(File.Open(filename + ".fft.dat", FileMode.Create)))
            {
                // ������ ��������� WAV-�����
                this.format = ReadHeader(reader);

                // ��������, ����� ���������� ��� �� ����� ���� ���� 8, ���� 16
                if (format.BitsPerSample != 8 && format.BitsPerSample != 16)
                {
                    System.Windows.Forms.MessageBox.Show("���� ����� " + format.BitsPerSample + " ��� �� �����. �������� ���� � 8 ��� 16 ������ �� �����.");
                    return;
                }

                int bytesPerSample = format.BitsPerSample / 8;  // ����������� ���������� ���� �� �����
                int dataLength = reader.ReadInt32();            // ����� ������
                int countSamples = dataLength / bytesPerSample; // ���������� �������

                int di = 5;
                int b = reader.ReadByte(); // ������ ������ �����
                int adv = 1;               // ������� ��������

                // ������� ������, ���� ��� �������� � �������� �� 120 �� 140
                while (b > 120 && b < 140)
                {
                    reader.ReadBytes(di - 1); // ������� ������
                    b = reader.ReadByte();
                    adv += di;
                }
                countSamples = countSamples - adv; // ��������� ���������� ������� � ������ ��������

                float[] channelSamples16 = new float[framelen]; // ����� ������ �� ���� ����

                // ����������� ���������� ����� ��� ������� ������
                int si = countSamples / (samples.Length * di);
                if (countSamples % (si * (samples.Length * di)) != 0)
                {
                    si++;
                }

                int overlap = (int)(0 * framelen); // �������� ��������� ������

                float[] sampelOverlap = null;
                if (overlap > 0)
                    sampelOverlap = new float[overlap]; // ����� ��� ���������� ������
                int nbsi = (framelen - overlap) / si;

                // ���������� ���������� ����� �������, ���� ����� ������������
                while (nbsi == 0)
                {
                    si = si - framelen;
                    nbsi = (framelen - overlap) / si;
                }
                if ((framelen - overlap) % (nbsi * si) != 0)
                {
                    nbsi++;
                }
                countSamples = countSamples - (overlap * di); // ��������� ���������� ������� � ������ ���������
                nbframe = countSamples / ((framelen - overlap) * di); // ���������� ������

                // ��������� ���������� ������, ���� ���� �������
                if (countSamples % ((framelen - overlap) * di) != 0)
                {
                    nbframe += 1;
                }

                A = new float[fftPoint / 2]; // ����� ��� ���������� FFT ������ �����
                int channelSamplesIndex = 0; // ������ ��� ������ � ������� �������
                int endloop = 0;
                byte channelSample8;
                Int16 sample16;
                int endfft = 0;

                // ��������� ������� (���������� ���������)
                firFilter.FreqFrom = 500;
                firFilter.FreqTo = 4000;
                firFilter.CalculateCoefficients(FFT.Algorithm.Blackman, FFT.FilterType.BandPass);

                // ������ ���������� �������
                channelSamplesIndex = 0;
                if (format.BitsPerSample == 8)
                {
                    for (int sampleIndex = 0; sampleIndex < overlap; sampleIndex++)
                    {
                        channelSample8 = reader.ReadByte(); // ������ 8-������� ������
                        sampelOverlap[channelSamplesIndex] = (short)((a[(short)(channelSample8)]) << 8); // �������������� � 16-������ ������
                        channelSamplesIndex++;
                        reader.ReadBytes(di - 1);
                        sampleIndex += di - 1;
                    }
                }
                else
                {
                    for (int sampleIndex = 0; sampleIndex < overlap; sampleIndex++)
                    {
                        sample16 = reader.ReadInt16(); // ������ 16-������� ������
                        sampelOverlap[channelSamplesIndex] = reader.ReadInt16();
                        channelSamplesIndex++;
                        reader.ReadBytes(2 * (di - 1));
                        sampleIndex += di - 1;
                    }
                }

                // ��������� ���������
                int progressInPercentage = 0;
                int factor = 100;
                float progressValue = 0;
                float dprogressvalue = (float)factor / (float)nbframe; // ��� ��� ���������� ���������
                while ((int)dprogressvalue >= 100 || (int)dprogressvalue == 0)
                {
                    factor *= 10;
                    dprogressvalue = factor / nbframe;
                }

                for (int i = 0; i < nbframe; i++)
                {
                    // ����������� ���������� ������ � �����
                    for (int sampleIndex = 0; sampleIndex < overlap; sampleIndex++)
                    {
                        channelSamples16[sampleIndex] = sampelOverlap[sampleIndex];
                    }
                    endloop = di * (channelSamples16.Length - overlap);

                    // ���� ��� ��������� ����, ������������ ���������� ������
                    if (i == (nbframe - 1))
                    {
                        endloop = countSamples % endloop;
                    }
                    channelSamplesIndex = overlap;

                    #region ������ ������

                    if (format.BitsPerSample == 8)
                    {
                        try
                        {
                            // ������ ������ ��� 8-������� �������
                            for (int sampleIndex = overlap; sampleIndex < (endloop + overlap) && channelSamplesIndex < framelen; sampleIndex++)
                            {
                                channelSample8 = reader.ReadByte();
                                channelSamples16[channelSamplesIndex] = (short)((a[(short)(channelSample8)]) << 8);
                                channelSamplesIndex++;
                                reader.ReadBytes(di - 1);
                                sampleIndex += di - 1;
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        try
                        {
                            // ������ ������ ��� 16-������� �������
                            for (int sampleIndex = 0; sampleIndex < countSamples && channelSamplesIndex < (framelen); sampleIndex++)
                            {
                                sample16 = reader.ReadInt16();
                                channelSamples16[channelSamplesIndex] = reader.ReadInt16();
                                channelSamplesIndex++;
                                reader.ReadBytes(2 * (di - 1));
                                sampleIndex += di - 1;
                            }
                        }
                        catch { }
                    }

                    #endregion // ������ ������

                    // ���������� ������ ��� �����������
                    for (int j = 0; j < nbsi && (j + i * nbsi) < samples.Length; j++)
                    {
                        samples[j + i * nbsi] = (short)channelSamples16[j * si];
                    }

                    if (i != (nbframe - 1))
                    {
                        // ���������� ������ ���������
                        for (int sampleIndex = framelen - overlap; sampleIndex < framelen; sampleIndex++)
                        {
                            sampelOverlap[sampleIndex - (framelen - overlap)] = channelSamples16[sampleIndex];
                        }
                    }

                    // ���������� �������
                    FilterCalculation(ref channelSamples16, FFT.FilterType.BandPass);

                    // ���������� FFT
                    ApplyFFT(channelSamples16);

                    endfft = fftPoint / 2;

                    // ����������� ������ ��� ������ � ���� FFT
                    if (channelSamplesIndex != 0)
                    {
                        if (endfft > channelSamplesIndex / 2)
                            endfft = channelSamplesIndex / 2;
                    }
                    for (int j = 1; j < endfft; j++)
                    {
                        fft.Write((Int16)A[j]);
                    }
                    fft.Write(-9999); // ��������� ����� ������ FFT

                    // ���������� ���������
                    progressValue += dprogressvalue;
                    if (progressInPercentage != (int)(((float)progressValue / (float)factor) * (MAX_PROGRESS * 0.9)))
                    {
                        progressInPercentage = (int)(((float)progressValue / (float)factor) * (MAX_PROGRESS * 0.9));
                        ((BackgroundWorker)sender).ReportProgress(progressInPercentage);
                    }
                }

                reader.Close();
                fft.Close();
            }
        }


        /// <summary>��������� ������ ���������� ��������� � ��������� ����������� ������� ���������� ����� ���������� ������ WAV-�����.</summary>
        /// <param name="sender">������� ����������, ��� �������� ����������� ��������</param> 
        /// <param name="e">��������� �������</param>
        /// <returns></returns>

        void bgw_WorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ProgressBar.Value = MAX_PROGRESS;
            GraphicControl.Refresh();
        }

        /// <summary>��������� ��������� ��������� ��� ������������ ������� ���������.</summary>
        /// <param name="sender">������� ����������, ��� �������� ����������� ��������</param> 
        /// <param name="e">��������� �������</param>
        /// <returns></returns>

        void bgw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBar.PerformStep();
        }

        /// <summary>������ ���� �� ������� ���� �� WAV-�����.</summary>
        /// <param name="reader">�������� ��� WAV-�����.</param>
        /// <returns>������ �������.</returns>

        private string ReadChunk(BinaryReader reader)
        {
            byte[] ch = new byte[4];
            reader.Read(ch, 0, ch.Length);
            return System.Text.Encoding.ASCII.GetString(ch,0,4);
        }

        /// <summary>
        /// ������ ��������� �� WAV-����� � ����������
        /// ������� �������� � ������ ����� ������.
        /// </summary>
        /// <param name="reader">�������� ��� WAV-�����.</param>
        /// <returns>������ WAV-�����.</returns>

        private WaveFormat ReadHeader(BinaryReader reader)
        {
            reader.BaseStream.Position = 0;
            if (ReadChunk(reader) != "RIFF")
                throw new Exception("Invalid file format");

            reader.ReadUInt32(); // ����� ����� ����� 8 ����, ��� RIFF ��������, ��� �� ������������
            if (ReadChunk(reader) != "WAVE")
                throw new Exception("Invalid file format");

            if (ReadChunk(reader) != "fmt ")
                throw new Exception("Invalid file format");

            uint fmtSize = reader.ReadUInt32();
            if (fmtSize < 16) // bad format chunk length
                throw new Exception("Invalid file format");

            WaveFormat carrierFormat = new WaveFormat();
            carrierFormat.FormatTag = reader.ReadInt16();
            carrierFormat.Channels = reader.ReadInt16();
            carrierFormat.SamplesPerSec = reader.ReadInt32();
            carrierFormat.AvgBytesPerSec = reader.ReadInt32();
            carrierFormat.BlockAlign = reader.ReadInt16();
            carrierFormat.BitsPerSample = reader.ReadInt16();

            if (fmtSize > 16)
            {
                reader.ReadBytes((int)fmtSize - 16);
            }
            string chunk = new string(reader.ReadChars(4));
            while (chunk != "data")
            {
                char next = reader.ReadChar();
                chunk = chunk.Substring(1, 3) + next;
            }
            return carrierFormat;
        }

        #endregion // Functions to process WAV

        #region Functions for FFT

        /// <summary>���������� ������� �������� ���� � ������ ������, ��������� �������� ���.</summary>
        /// <param name="wf">�������� ����, � ������� ���������� ���������</param> 
        /// <returns>������� ��������</returns>
        public float Compare(WaveSound wf)
        {
            int nbcorrect = 0; // ���������� ����������� �������������
            int i = 0;
            bool endfile = false; // ���� ��������� �����
            int a = 0, b = 0; // ��������� ���������� ��� �������� ������ �� ������
            float[] frames = new float[nbframe > wf.nbframe ? nbframe : wf.nbframe]; // ������ ��� �������� �������� �� ������
            comparedFrame = nbframe < wf.nbframe ? nbframe : wf.nbframe; // ���������� ������ ��� ���������
            short[] MissPlay = new short[comparedFrame]; // ������ ��� �������� ������� ������ � �������������
            int step = 0;
            if (ProgressBar.Value != MAX_PROGRESS)
                step = (MAX_PROGRESS - ProgressBar.Value) / comparedFrame;

            // �������� ������ � ������� ���
            using (BinaryReader f1 = new BinaryReader(File.Open(filename + ".fft.dat", FileMode.Open)))
            using (BinaryReader f2 = new BinaryReader(File.Open(wf.filename + ".fft.dat", FileMode.Open)))
            using (StreamWriter sw2 = new StreamWriter(wf.filename + ".fft.txt"))
            {
                while (!endfile)
                {
                    try
                    {
                        a = f1.ReadInt16(); // ������ ������ �� ������� �����
                        b = f2.ReadInt16(); // ������ ������ �� ������� �����
                        if (a == -9999 || b == -9999)
                        {
                            // ������������ ������� ���������� ��� ������ �����
                            frames[i] = (float)nbcorrect / (fftPoint / 2);
                            ProgressBar.Value += step; // ��������� ��������
                            i++;
                            nbcorrect = 0;
                        }
                        else
                        {
                            // ���������� ������������, ���� ��� ���������� �� ����� ��� �� 2, �� ������� �� �����������
                            if (Math.Abs(a - b) <= 2)
                            {
                                nbcorrect++;
                            }
                        }
                        sw2.Write(a); // ���������� ������ ��� ����������� �������
                        sw2.WriteLine("," + b);
                    }
                    catch
                    {
                        endfile = true; // ���� �������� ����� �����
                        f1.Close();
                        f2.Close();
                        sw2.Close();
                    }
                }

                nbcorrect = 0;
                int idx = 0;
                int len = comparedFrame;
                comparedFrame = 0;

                // ����������� ����� � ����������, ��������� ��� �����
                for (i = 0; i < len; i++)
                {
                    nbcorrect += frames[i] >= 0.7 ? 1 : 0; // ���� ���� ���� ����� ��� �� 70%, ����������� �������
                    if (frames[i] < 0.7)
                    {
                        MissPlay[idx] = (short)i; // ���������� ������ ������ � �������������
                        idx++;
                        comparedFrame++;
                    }
                }

                f1.Close();
                f2.Close();
                sw2.Close();

                // ���������� ������ ������, ��� ���� �����������
                for (i = 0; i < comparedFrame; i++)
                    missData = missData + MissPlay[i] + ";";
            }

            return (float)(nbcorrect * 100 / frames.Length); // ���������� ������� ��������
        }

        /// <summary>�������� ��������� ������ ������ �� �����</summary>
        List<int> GetNextFrames(int count, BinaryReader reader)
        {
            List<int> frames = new List<int>();
            for (int i = 0; i < count; i++)
            {
                frames.AddRange(GetNextFrame(reader)); // ��������� �� ������
            }
            return frames;
        }

        /// <summary>�������� ���� ���� ������ �� �����</summary>
        List<int> GetNextFrame(BinaryReader reader)
        {
            List<int> frame = new List<int>();
            int coef = reader.ReadInt16(); // ������ ������������
            while (coef != -9999 && reader.BaseStream.Position < reader.BaseStream.Length - 1)
            {
                frame.Add(coef); // ��������� ������������ � ������
                coef = reader.ReadInt16();
            }
            return frame;
        }

        /// <summary>���������� ��������� ���</summary>
        /// <param name="data">������, ������� ����� ���������� ����� ���</param> 
        public void ApplyFFT(float[] data)
        {
            float[] a2 = new float[fftPoint]; // ��������� ������� ��� �������� �������� � ������ ������
            float[] a1 = new float[fftPoint];
            int loop = data.Length / fftPoint; // ���������� �������� ��� ��������� ���� ������
            if ((loop * fftPoint) < data.Length)
                loop += 1;
            int inloop, k = 0, stop, t;
            int log2n = ilog2(fftPoint); // �������� �� ����� ���
            float maxA = float.MinValue, minA = float.MaxValue;
            float refA = 0;

            for (int i = 0; i < loop; i++)
            {
                k = 0;
                inloop = i * fftPoint;
                stop = inloop + fftPoint;
                if (stop > data.Length)
                    stop = data.Length;

                // ������������ ������ � ��������� �� �������� � ������ �����
                for (int j = inloop; j < stop; j++)
                {
                    t = bitrev(k, log2n); // ������� ��������
                    a1[t] = data[j];
                    a2[t] = 0.0f;
                    k++;
                }

                // ��������� ���������� ������ ������
                if (stop % fftPoint != 0)
                {
                    for (int l = stop % fftPoint; l < fftPoint; l++)
                    {
                        t = bitrev(l, log2n);
                        a1[t] = 0f;
                        a2[t] = 0.0f;
                    }
                }

                fft(log2n, ref a2, ref a1, fftPoint, 1); // ��������� ���

                k = 0;
                a1[0] = 0; a2[0] = 0; // �������� ������� ������������
                maxA = float.MinValue;
                minA = float.MaxValue;

                // ������������ ���������
                for (int v = inloop / 2; v < stop / 2; v++)
                {
                    A[v] = (a1[k] * a1[k]) + (a2[k] * a2[k]);
                    maxA = Math.Max(A[v], maxA);
                    minA = Math.Min(A[v], minA);
                    k++;
                }

                refA = maxA - minA; // ������ ������������ ��������

                // ����������� ��������� � ��������
                for (int v = inloop / 2 + 1; v < stop / 2; v++)
                    A[v] = 10f * (float)Math.Log10(A[v] / refA);
                for (int v = stop / 2; v < fftPoint / 2; v++)
                    A[v] = 0;
            }
        }

        /// <summary>���������� ���</summary>
        void fft(int log2n, ref float[] a2, ref float[] a1, int n, int sgn)
        {
            int i, j, k, k2, s, m;
            float wm1, wm2, w1, w2, t1, t2, u1, u2;

            // �������� �� ������� ���
            for (s = 1; s <= log2n; s++)
            {
                m = 1 << s; // m = 2^s
                wm1 = (float)Math.Cos(sgn * 2 * Math.PI / m); // ������ ��������� ����������
                wm2 = (float)Math.Sin(sgn * 2 * Math.PI / m);

                w1 = 1.0f;
                w2 = 0.0f;

                for (j = 0; j < m / 2; j++)
                {
                    for (k = j; k < n; k += m)
                    {
                        k2 = k + m / 2;
                        t1 = w1 * a1[k2] - w2 * a2[k2];
                        t2 = w1 * a2[k2] + w2 * a1[k2];

                        u1 = a1[k];
                        u2 = a2[k];

                        a1[k] = u1 + t1;
                        a2[k] = u2 + t2;

                        a1[k2] = u1 - t1;
                        a2[k2] = u2 - t2;
                    }

                    // ��������� ��������� ���������
                    t1 = w1 * wm1 - w2 * wm2;
                    w2 = w1 * wm2 + w2 * wm1;
                    w1 = t1;
                }
            }

            // ��������� ��������� ������
            for (i = 1; i < n / 2; i++)
            {
                t1 = a1[i];
                a1[i] = a1[n - i];
                a1[n - i] = t1;
                t2 = a2[i];
                a2[i] = a2[n - i];
                a2[n - i] = t2;
            }

            // ���� �������� ��������������, ����� �� n
            if (sgn == -1)
            {
                for (i = 0; i < n; i++)
                {
                    a1[i] /= (float)n;
                    a2[i] /= (float)n;
                }
            }
        }

        /// <summary>�������� �� ��������� 2</summary>
        private int ilog2(int n)
        {
            int i;
            for (i = 8 * sizeof(int) - 1; i >= 0 && ((1 << i) & n) == 0; i--) ;
            return i;
        }

        /// <summary>�������� ����� � ����� "a" ��� ��������� 0 �� k-1</summary>
        int bitrev(int a, int k)
        {
            int i, b, p, q;
            for (i = b = 0, p = 1, q = 1 << (k - 1);
                 i < k;
                 i++, p <<= 1, q >>= 1) if ((a & q) > 0) b |= p;
            return b;
        }

        #endregion // Functions for FFT


        #region Functions for Digital Filter

        /// <summary>
        /// Performs the actual filtering operation with respect to input from the combo boxes.
        /// </summary>
        private void FilterCalculation(ref float[] input, FFT.FilterType filter)
        {            
            // Check what kind of filter the user wanted and filter accordingly
            switch (filter)
            {
                case FFT.FilterType.HighPass:
                    firFilter.HighPassFilter(ref input);
                    break;

                case FFT.FilterType.LowPass:
                    firFilter.LowPassFilter(ref input);
                    break;

                case FFT.FilterType.BandPass:
                    firFilter.BandPassFilter(ref input);
                    break;
            }
        }

        #endregion // Functions for Digital Filter
    }
}
