using System;

namespace SoundComparer.DigitalFilter
{
    /// <summary>
    /// ��������������� �����, ������� ��������� ��������� ������� ������� ��� ����������� ������������� �������.
    /// </summary>
    class FFT
    {
        #region Members

        /// <summary>
        /// ������������ ����� �������� ��� ������������� ���� �������, ��� �������� ��� ����� ������������.
        /// </summary>
        public enum FilterType { HighPass, LowPass, BandPass };

        /// <summary>
        /// ������������ ���������� ��� ������ ���������.
        /// </summary>
        public enum Algorithm { Kaiser, Hann, Hamming, Blackman, Rectangular };

        private float myRate;  // ������� �������������
        private float myFreqFrom;  // ��������� ������� ������ �����������
        private float myFreqTo;  // �������� ������� ������ �����������
        private float myAttenuation;  // ���������� � ������ ����������
        private float myBand;  // ������ ��������
        private float myAlpha;  // ����� ��� ��������� �������
        private int myOrder;  // ������� �������

        /// <summary>
        /// ������� ������������� �� ������� (�������� ������� �������������)
        /// </summary>
        private float myFS;

        #endregion // Members

        #region Properties

        /// <summary>
        /// ������� �������������.
        /// </summary>
        public float Rate
        {
            get { return myRate; }
            set
            {
                myRate = value;
                myFS = 0.5f * myRate;  // ��������� ������� �������
            }
        }

        /// <summary>
        /// ��������� ������� ������ �����������. ������ ���� ������ �������� �������.
        /// </summary>
        public float FreqFrom
        {
            get { return myFreqFrom; }
            set { myFreqFrom = value; }
        }

        /// <summary>
        /// �������� ������� ������ �����������. ������ ���� ������ ��������� �������.
        /// </summary>
        public float FreqTo
        {
            get { return myFreqTo; }
            set { myFreqTo = value; }
        }

        /// <summary>
        /// ���������� � ������ ����������.
        /// </summary>
        public float StopBandAttenuation
        {
            get { return myAttenuation; }
            set { myAttenuation = value; }
        }

        /// <summary>
        /// ������ ��������.
        /// </summary>
        public float TransitionBand
        {
            get { return myBand; }
            set { myBand = value; }
        }

        /// <summary>
        /// �������� �����, ������������ ��� ��������� �������.
        /// </summary>
        public float Alpha
        {
            get { return myAlpha; }
            set { myAlpha = value; }
        }

        /// <summary>
        /// ������� �������. ������ ���� ������ ������.
        /// </summary>
        public int Order
        {
            get { return myOrder; }
            set { myOrder = value; }
        }

        #endregion // Properties

        #region Constructors

        /// <summary>
        /// ����������� ��� �������� ���������� FFT � ������������� ���������� �� ���������.
        /// </summary>
        public FFT()
        {
            // ������� �� ��������� 8000 ��
            Rate = 11025;

            // ���������� �� ��������� 60 ��
            this.myAttenuation = 60;

            // ������ �������� �� ��������� 500 ��
            this.myBand = 500;

            // ������� ������� �� ��������� 0, ����� �����, ��� �� �� ������� �������������
            this.myOrder = 0;

            // ����� �� ��������� 4
            this.myAlpha = 4;
        }

        #endregion // Constructors

        #region Mathematical Functions

        /// <summary>
        /// ������� ������� �������� �������, ������� ������������ � ���� �������.
        /// ��� �������������� ������������� ���������������� ������� ������� �������� �������.
        /// </summary>
        /// <param name="x">�����, ��� �������� ����� ���������� ������� �������</param>
        private float Bessel(float x)
        {
            double ax, ans;
            double y;

            ax = System.Math.Abs(x);  // ������ �����
            if (ax < 3.75)
            {
                y = x / 3.75;
                y *= y;
                ans = 1.0 + y * (3.5156229 + y * (3.0899424 + y * (1.2067492
                    + y * (0.2659732 + y * (0.360768e-1 + y * 0.45813e-2)))));
            }
            else
            {
                y = 3.75 / ax;
                ans = (System.Math.Exp(ax) / System.Math.Sqrt(ax)) * (0.39894228 + y * (0.1328592e-1
                    + y * (0.225319e-2 + y * (-0.157565e-2 + y * (0.916281e-2
                    + y * (-0.2057706e-1 + y * (0.2635537e-1 + y * (-0.1647633e-1
                    + y * 0.392377e-2))))))));
            }

            return (float)ans;
        }

        #endregion // Mathematical Functions

        #region Generate Coefficients

        /// <summary>
        /// ���������� �������������, ������������ ��������.
        /// </summary>
        /// <param name="filterType">������������ ���� �������, ������� ����� �����������.</param>
        /// <param name="alg">������������ ���������, ������� ����� �������������� ��� �������� ���������.</param>
        public float[] GenerateCoefficients(FilterType filterType, Algorithm alg)
        {
            // ������������ ������� �������, ���� �� �� ��� ����������
            if (this.myOrder == 0)
                this.myOrder = (int)(((this.myAttenuation - 7.95f) / (this.myBand * 14.36f / this.myFS) + 1.0f) * 2.0f) - 1;

            float[] window = new float[(this.myOrder / 2) + 1];  // ������� �������
            float[] coEff = new float[this.myOrder + 1];  // ������������ �������
            float ps;
            float pe;
            const float PI = (float)System.Math.PI;
            int o2 = this.myOrder / 2;

            // ������������� � ����������� �� ���������
            switch (alg)
            {
                case Algorithm.Kaiser:
                    // ������� ������� �������
                    for (int i = 1; i <= o2; i++)
                    {
                        window[i] = Bessel(this.myAlpha * (float)System.Math.Sqrt(1.0f - (float)System.Math.Pow((float)i / o2, 2))) / Bessel(this.myAlpha);
                    }

                    // ���������� � ������ ���������� � ������ �������� ������ ���� ����������� �������������
                    break;

                case Algorithm.Hann:
                    // ������� ������� �����
                    for (int i = 1; i <= o2; i++)
                    {
                        window[i] = 0.5f + 0.5f * (float)System.Math.Cos((PI / (o2 + 1)) * i);
                    }

                    // ������������� ����������� ���������� � ������ ����������
                    this.StopBandAttenuation = 44.0f;

                    // ������������� ������ ��������
                    this.TransitionBand = 6.22f * this.myFS / this.myOrder;
                    break;

                case Algorithm.Hamming:
                    // ������� ������� ��������
                    for (int i = 1; i <= o2; i++)
                    {
                        window[i] = 0.54f + 0.46f * (float)System.Math.Cos((PI / o2) * i);
                    }

                    // ������������� ����������� ���������� � ������ ����������
                    this.StopBandAttenuation = 53.0f;

                    // ������������� ������ ��������
                    this.TransitionBand = 6.64f * this.myFS / this.myOrder;
                    break;

                case Algorithm.Blackman:
                    // ������� ������� ��������
                    for (int i = 1; i <= o2; i++)
                    {
                        window[i] = 0.42f + 0.5f * (float)Math.Cos((PI / o2) * i) + 0.08f * (float)Math.Cos(2.0f * (PI / o2) * i);
                    }

                    // ������������� ����������� ���������� � ������ ����������
                    this.StopBandAttenuation = 74.0f;

                    // ������������� ������ ��������
                    this.TransitionBand = 11.13f * this.myFS / this.myOrder;
                    break;

                case Algorithm.Rectangular:
                    // ������������� ������� �������
                    for (int i = 1; i <= o2; i++)
                    {
                        window[i] = 1.0f;
                    }

                    // ������������� ����������� ���������� � ������ ����������
                    this.StopBandAttenuation = 21.0f;

                    // ������������� ������ ��������
                    this.TransitionBand = 1.84f * this.myFS / this.myOrder;
                    break;

                default:
                    // �������� ��� ��������, ���� ������ �� ���� ����������� (������)
                    for (int i = 1; i <= o2; i++)
                    {
                        window[i] = 0.0f;
                    }
                    break;
            }

            // ������������� � ����������� �� ���� �������
            switch (filterType)
            {
                case FilterType.BandPass:
                    pe = PI / 2 * (this.FreqTo - this.FreqFrom + this.myBand) / this.myFS;
                    ps = PI / 2 * (this.FreqFrom + this.FreqTo) / this.myFS;
                    break;

                case FilterType.LowPass:
                    pe = PI * (this.FreqTo + this.myBand / 2) / this.myFS;
                    ps = 0.0f;
                    break;

                case FilterType.HighPass:
                    pe = PI * (1.0f - (this.FreqFrom - this.myBand / 2) / this.myFS);
                    ps = PI;
                    break;

                default:
                    pe = 0.0f;
                    ps = 0.0f;
                    break;
            }

            // ������������� ������ �������� ������������
            coEff[0] = pe / PI;

            // ��������� ��������� ������������
            for (int i = 1; i <= o2; i++)
            {
                coEff[i] = window[i] * (float)System.Math.Sin(i * pe) * (float)System.Math.Cos(i * ps) / (i * PI);
            }

            // ������� �������
            for (int i = o2 + 1; i <= this.myOrder; i++)
            {
                coEff[i] = coEff[i - o2];
            }
            for (int i = 0; i <= o2 - 1; i++)
            {
                coEff[i] = coEff[this.myOrder - i];
            }
            coEff[o2] = pe / PI;

            return coEff;
        }

        #endregion // Generate Coefficients
    }
}
