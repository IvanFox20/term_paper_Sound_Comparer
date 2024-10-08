using System;

namespace SoundComparer.DigitalFilter
{
    /// <summary>
    /// ��������������� �����, ������� ��������� ������� ����������. � ��������� �����
    /// ����������� ��������������, ��������������� � ��������� �������.
    /// </summary>
    class FIRFilters
    {
        #region Members

        /// <summary>
        /// ���������� �������� ����� ��, ��� � ���������� �����
        /// </summary>
        private int mySamples;

        /// <summary>
        /// ������ ������������� ������� �������
        /// </summary>
        private float[] myCoeff;

        /// <summary>
        /// ������, ������� �������� ������� ������
        /// </summary>
        private float[] myInputSeries;

        /// <summary>
        /// ������ ��� ���������� ��������� ��� (�������� �������������� �����)
        /// </summary>
        private FFT myFFT;

        /// <summary>
        /// ������� ��������� ��������. ��� ������������ ���� �� ������� ���.
        /// </summary>
        public FFT.Algorithm CurrentAlgorithm;

        /// <summary>
        /// ������� ��� �������
        /// </summary>
        public FFT.FilterType CurrentFilter;

        private float myFreqFrom;   // ������� ������ ������ �����������
        private float myFreqTo;     // ������� ����� ������ �����������
        private float myAttenuation; // ���������� � ������ ����������
        private float myBand;        // ������ ��������
        private float myAlpha;       // �������� ����� ��� ������� ������� �������
        private int myTaps;          // ���������� ��������
        private int myOrder;         // ������� ������� (������ ���� ������)

        #endregion // Members

        #region Properties

        /// <summary>
        /// ������� ������ ������ �����������, ������ ���� ���� ������� ���������.
        /// </summary>
        public float FreqFrom
        {
            get { return myFreqFrom; }
            set { myFreqFrom = value; }
        }

        /// <summary>
        /// ������� ��������� ������ �����������, ������ ���� ���� ������� ������.
        /// </summary>
        public float FreqTo
        {
            get { return myFreqTo; }
            set { myFreqTo = value; }
        }

        /// <summary>
        /// ���������� � ������ ����������
        /// </summary>
        public float StopBandAttenuation
        {
            get { return myAttenuation; }
            set
            {
                myAttenuation = value;
                this.myFFT.StopBandAttenuation = myAttenuation;
            }
        }

        /// <summary>
        /// ������ ��������
        /// </summary>
        public float TransitionBand
        {
            get { return myBand; }
            set
            {
                myBand = value;
                this.myFFT.TransitionBand = myBand;
            }
        }

        /// <summary>
        /// �������� ����� ��� ��������� �������.
        /// </summary>
        public float Alpha
        {
            get { return myAlpha; }
            set
            {
                myAlpha = value;
                this.myFFT.Alpha = myAlpha;
            }
        }

        /// <summary>
        /// ���������� �������� (taps), ������� ������������. ��� ���������� �������, �������������� �� ���.
        /// </summary>
        public int Taps
        {
            get { return myTaps; }
            set { myTaps = value; }
        }

        /// <summary>
        /// ������� �������. ������ ���� ������ ������.
        /// </summary>
        public int Order
        {
            get { return myOrder; }
            set
            {
                // ���������, ��� �������� ������
                if ((value % 2) == 0)
                {
                    myOrder = value;
                    this.myFFT.Order = myOrder;
                }
                else
                    throw new ArgumentOutOfRangeException("Order", "������� ������� ������ ���� ������ ������.");
            }
        }

        #endregion // Properties

        #region Constructors

        /// <summary>
        /// �������� �����������. ���������� ��� ��������� ������ ������� ���.
        /// </summary>
        public FIRFilters()
        {
            // ������� ����� ������ ��� ���
            this.myFFT = new FFT();

            // �������� �� ��������� � ������
            this.CurrentAlgorithm = FFT.Algorithm.Kaiser;

            // ���������� �������� �� ��������� ����� 35
            this.myTaps = 35;
        }

        #endregion // Constructors

        #region Methods

        /// <summary>
        /// ��������� �������������� ������. �������� ������ ����� ������ ����� ������� � ���� ������.
        /// ���� ��������� � �������� ������� ������ ����������� ����� 0, ������������ �������� �� ���������.
        /// </summary>
        public void LowPassFilter(ref float[] iseries)
        {
            // ���� �� ������ ��������� � �������� �������, ���������� �������� �� ��������� 0 - 1000 ��
            if (this.myFreqFrom == 0.0f && this.myFreqTo == 0.0f)
            {
                this.myFFT.FreqFrom = 0.0f;
                this.myFFT.FreqTo = 1000.0f;
            }
            else
            {
                this.myFFT.FreqFrom = this.myFreqFrom;
                this.myFFT.FreqTo = this.myFreqTo;
            }

            // ��������� ������ � ����� ������ �� ������ ��������������� �������������
            Filter(ref iseries);
        }

        /// <summary>
        /// ��������� ������������ ������� � ����������� �� ���������� ��������� � ���� �������.
        /// </summary>
        public void CalculateCoefficients(FFT.Algorithm algorithm, FFT.FilterType filter)
        {
            // ���������� ������������
            this.CurrentAlgorithm = algorithm;
            this.CurrentFilter = filter;
            if (algorithm == FFT.Algorithm.Kaiser)
            {
                // ��� ������� ������� ������� ���������� ������ ����������
                this.StopBandAttenuation = 60;

                // ����� ��������� ������ ������ ��������
                this.TransitionBand = 500;
            }

            // ���� ������� �� ������, ���������� �������� �� ��������� 1000 ��
            if (this.myFreqFrom == 0.0f && this.myFreqTo == 0.0f)
            {
                this.myFFT.FreqFrom = 1000;
                this.myFFT.FreqTo = 1000f;
            }
            else
            {
                this.myFFT.FreqFrom = this.myFreqFrom;
                this.myFFT.FreqTo = this.myFreqTo;
            }

            // ���������� ������������
            this.myCoeff = this.myFFT.GenerateCoefficients(this.CurrentFilter, this.CurrentAlgorithm);
        }

        /// <summary>
        /// ��������� ��������������� ������. �������� ������ ����� ������ ����� ������� � ���� ������.
        /// ���� ��������� � �������� ������� ������ ����������� ����� 0, ������������ �������� �� ���������.
        /// </summary>
        public void HighPassFilter(ref float[] iseries)
        {
            // ���� �� ������ ��������� � �������� �������, ���������� �������� �� ��������� 2000 - 4000 ��
            if (this.myFreqFrom == 0.0f && this.myFreqTo == 0.0f)
            {
                this.myFFT.FreqFrom = 2000.0f;
                this.myFFT.FreqTo = 4000.0f;
            }
            else
            {
                this.myFFT.FreqFrom = this.myFreqFrom;
                this.myFFT.FreqTo = this.myFreqTo;
            }

            // ��������� ������ � ����� ������ �� ������ ��������������� �������������
            Filter(ref iseries);
        }

        /// <summary>
        /// ��������� ��������� ������. �������� ������ ����� ������ ����� ������� � ���� ������.
        /// ���� ��������� � �������� ������� ������ ����������� ����� 0, ������������ �������� �� ���������.
        /// </summary>
        public void BandPassFilter(ref float[] iseries)
        {
            // ���� �� ������ ��������� � �������� �������, ���������� �������� �� ��������� 1000 - 1000 ��
            if (this.myFreqFrom == 0.0f && this.myFreqTo == 0.0f)
            {
                this.myFFT.FreqFrom = 1000;
                this.myFFT.FreqTo = 1000f;
            }
            else
            {
                this.myFFT.FreqFrom = this.myFreqFrom;
                this.myFFT.FreqTo = this.myFreqTo;
            }

            // ��������� ������ � ����� ������ �� ������ ��������������� �������������
            Filter(ref iseries);
        }

        #endregion // Methods

        #region Initialization

        /// <summary>
        /// �������������� ������ FIRFilters, ������������ ������� ������ ��� ������������� ��������.
        /// </summary>
        /// <param name="iseries">������� ������, ���������� �������� ������</param>
        private void SetIOSeries(float[] iseries)
        {
            this.myInputSeries = iseries;

            // ���������� �������� � ��� ���������� �����, ������������ �� ������� ������
            this.mySamples = myInputSeries.Length;
        }

        #endregion // Initialization

        #region Filter

        /// <summary>
        /// ��������� ���� ����������. ������������ ������ ���� ������� ������������� ���������� ��������,
        /// ������ ������� ������ ��������� �� � ��������� ����� � �������� ������.
        /// </summary>
        private void Filter(ref float[] iseries)
        {
            float[] x = new float[myTaps];  // ��������������� ������ ��� �������� �������
            float y;  // ���������� ��� ���������� ����� ���������� ����������

            // ������������� ������� ����� ������
            SetIOSeries(iseries);

            // �������������� ������ x ������
            for (int i = 1; i < myTaps; i++)
                x[i] = 0.0f;

            // ���� �� ������ ����� ������� ����� ������
            for (int i = 0; i < iseries.Length; i++)
            {
                y = 0.0f;  // �������������� ���������� �����

                // �������� ������� �������� ������
                x[0] = Convert.ToSingle(iseries[i]);

                // ���� �� ������������� ������� � ������ ����� ������������
                try
                {
                    for (int j = 0; j < myTaps; j++)
                        y = y + (x[j] * myCoeff[j]);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message + " ��������� ������� �������.");
                    throw e;
                }

                // �������� �������� � ������� x ������
                for (int j = myTaps - 1; j > 0; j--)
                    x[j] = x[j - 1];

                // ���������� ��������� � �������� ������ �� ������� �������� x
                iseries[i] = y;
            }
        }

        #endregion // Filter
    }
}
