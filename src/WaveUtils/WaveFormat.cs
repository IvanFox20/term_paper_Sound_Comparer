using System.Runtime.InteropServices;

namespace SoundComparer.WaveUtils
{
    /// <summary>
    /// ������������ �������� WAV ������. 
    /// PCM ������������ ��� ������� �������� �����������,
    /// � Float ��� ����������� � ��������� ������.
    /// </summary>
    public enum WaveFormats
    {
        Pcm = 1,   // ������ PCM
        Float = 3  // ������ � ��������� ������
    }

    /// <summary>
    /// �����, �������������� ��������� ������� RIFF Wave �����.
    /// �� �������� ���������� � ���������� �����, ����� ��� ������� �������������,
    /// ���������� �������, ������� �������� �������� ������ � �.�.
    /// </summary>
	[StructLayout(LayoutKind.Sequential)]
    public class WaveFormat
    {
        #region Members

        // ���� wFormatTag: ���������� ������ ����������� (��������, PCM ��� Float)
        private short wFormatTag;

        // ���� nChannels: ���������� ������� (1 ��� ����, 2 ��� ������ � �.�.)
        private short nChannels;

        // ���� nSamplesPerSec: ������� ������������� (���������� ������� � �������)
        private int nSamplesPerSec;

        // ���� nAvgBytesPerSec: ������� �������� �������� ������ (���� � �������)
        private int nAvgBytesPerSec;

        // ���� nBlockAlign: ������ ����� � ������
        private short nBlockAlign;

        // ���� wBitsPerSample: ���������� ��� �� �������
        private short wBitsPerSample;

        // ���� cbSize: �������������� ���������� � �������, ������������ ��� ����������� ��������
        private short cbSize;

        #endregion // Members

        #region Properties

        /// <summary>
        /// ���� wFormatTag: ���������� ������ ����������� (��������, PCM ��� Float).
        /// </summary>
        public short FormatTag
        {
            get { return wFormatTag; }
            set { wFormatTag = value; }
        }

        /// <summary>
        /// ���� nChannels: ���������� ���������� ������� (����, ������ � �.�.).
        /// </summary>
        public short Channels
        {
            get { return nChannels; }
            set { nChannels = value; }
        }

        /// <summary>
        /// ���� nSamplesPerSec: ���������� ������� ������������� (���������� ������� � �������).
        /// </summary>
        public int SamplesPerSec
        {
            get { return nSamplesPerSec; }
            set { nSamplesPerSec = value; }
        }

        /// <summary>
        /// ���� nAvgBytesPerSec: ������� �������� �������� ������ (� ������ �� �������).
        /// </summary>
        public int AvgBytesPerSec
        {
            get { return nAvgBytesPerSec; }
            set { nAvgBytesPerSec = value; }
        }

        /// <summary>
        /// ���� nBlockAlign: ���������� ������ ����� � ������.
        /// </summary>
        public short BlockAlign
        {
            get { return nBlockAlign; }
            set { nBlockAlign = value; }
        }

        /// <summary>
        /// ���� wBitsPerSample: ���������� ��� �� �������.
        /// </summary>
        public short BitsPerSample
        {
            get { return wBitsPerSample; }
            set { wBitsPerSample = value; }
        }

        /// <summary>
        /// ���� cbSize: �������������� ���������� � �������. 
        /// ������������ ��� ����������� �������� WAV.
        /// </summary>
        public short Size
        {
            get { return cbSize; }
            set { cbSize = value; }
        }

        #endregion // Properties

        #region Constructors

        /// <summary>
        /// ����������� �� ���������. �������������� ������ ������ WaveFormat.
        /// </summary>
        public WaveFormat()
        {
            // ����������� �� ���������
        }

        /// <summary>
        /// ����������� � �����������. ������������� ������� �������������, ���������� ��� �� ������� � ���������� �������.
        /// </summary>
        /// <param name="samplesPerSec">���������� ������� � ������� (������� �������������).</param>
        /// <param name="bitsPerSample">���������� ��� �� �������.</param>
        /// <param name="channels">���������� ������� (����, ������ � �.�.).</param>
		public WaveFormat(int samplesPerSec, short bitsPerSample, short channels)
        {
            // ������������� ������ � PCM
            wFormatTag = (short)WaveFormats.Pcm;

            // ������������� ���������� �������
            nChannels = channels;

            // ������������� ������� �������������
            nSamplesPerSec = samplesPerSec;

            // ������������� ���������� ��� �� �������
            wBitsPerSample = bitsPerSample;

            // �������������� ������ ����������� (cbSize = 0)
            cbSize = 0;

            // ������������ ������ ����� (���������� ���� �� ������� ��� ������� ������)
            nBlockAlign = (short)(channels * (bitsPerSample / 8));

            // ������������ ������� �������� �������� ������ (� ������ �� �������)
            nAvgBytesPerSec = samplesPerSec * nBlockAlign;
        }

        #endregion // Constructors
    }
}
