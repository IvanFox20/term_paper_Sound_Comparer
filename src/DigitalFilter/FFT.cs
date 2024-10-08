using System;

namespace SoundComparer.DigitalFilter
{
    /// <summary>
    /// Вспомогательный класс, который реализует различные оконные функции для определения коэффициентов фильтра.
    /// </summary>
    class FFT
    {
        #region Members

        /// <summary>
        /// Перечисление типов фильтров для идентификации типа фильтра, для которого нам нужны коэффициенты.
        /// </summary>
        public enum FilterType { HighPass, LowPass, BandPass };

        /// <summary>
        /// Перечисление алгоритмов для выбора алгоритма.
        /// </summary>
        public enum Algorithm { Kaiser, Hann, Hamming, Blackman, Rectangular };

        private float myRate;  // Частота дискретизации
        private float myFreqFrom;  // Начальная частота полосы пропускания
        private float myFreqTo;  // Конечная частота полосы пропускания
        private float myAttenuation;  // Ослабление в полосе подавления
        private float myBand;  // Полоса перехода
        private float myAlpha;  // Альфа для алгоритма Кайзера
        private int myOrder;  // Порядок фильтра

        /// <summary>
        /// Частота дискретизации по Шеннону (половина частоты дискретизации)
        /// </summary>
        private float myFS;

        #endregion // Members

        #region Properties

        /// <summary>
        /// Частота дискретизации.
        /// </summary>
        public float Rate
        {
            get { return myRate; }
            set
            {
                myRate = value;
                myFS = 0.5f * myRate;  // Установка частоты Шеннона
            }
        }

        /// <summary>
        /// Начальная частота полосы пропускания. Должна быть меньше конечной частоты.
        /// </summary>
        public float FreqFrom
        {
            get { return myFreqFrom; }
            set { myFreqFrom = value; }
        }

        /// <summary>
        /// Конечная частота полосы пропускания. Должна быть больше начальной частоты.
        /// </summary>
        public float FreqTo
        {
            get { return myFreqTo; }
            set { myFreqTo = value; }
        }

        /// <summary>
        /// Ослабление в полосе подавления.
        /// </summary>
        public float StopBandAttenuation
        {
            get { return myAttenuation; }
            set { myAttenuation = value; }
        }

        /// <summary>
        /// Полоса перехода.
        /// </summary>
        public float TransitionBand
        {
            get { return myBand; }
            set { myBand = value; }
        }

        /// <summary>
        /// Значение альфа, используемое для алгоритма Кайзера.
        /// </summary>
        public float Alpha
        {
            get { return myAlpha; }
            set { myAlpha = value; }
        }

        /// <summary>
        /// Порядок фильтра. Должен быть четным числом.
        /// </summary>
        public int Order
        {
            get { return myOrder; }
            set { myOrder = value; }
        }

        #endregion // Properties

        #region Constructors

        /// <summary>
        /// Конструктор для создания экземпляра FFT и инициализации значениями по умолчанию.
        /// </summary>
        public FFT()
        {
            // Частота по умолчанию 8000 Гц
            Rate = 11025;

            // Ослабление по умолчанию 60 дБ
            this.myAttenuation = 60;

            // Полоса перехода по умолчанию 500 Гц
            this.myBand = 500;

            // Порядок фильтра по умолчанию 0, чтобы знать, был ли он изменен пользователем
            this.myOrder = 0;

            // Альфа по умолчанию 4
            this.myAlpha = 4;
        }

        #endregion // Constructors

        #region Mathematical Functions

        /// <summary>
        /// Функция Бесселя нулевого порядка, которая используется в окне Кайзера.
        /// Это полиномиальная аппроксимация модифицированной функции Бесселя нулевого порядка.
        /// </summary>
        /// <param name="x">Число, для которого будет рассчитана функция Бесселя</param>
        private float Bessel(float x)
        {
            double ax, ans;
            double y;

            ax = System.Math.Abs(x);  // Модуль числа
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
        /// Вычисление коэффициентов, используемых фильтром.
        /// </summary>
        /// <param name="filterType">Перечисление типа фильтра, который будет применяться.</param>
        /// <param name="alg">Перечисление алгоритма, который будет использоваться для оконного алгоритма.</param>
        public float[] GenerateCoefficients(FilterType filterType, Algorithm alg)
        {
            // Рассчитываем порядок фильтра, если он не был установлен
            if (this.myOrder == 0)
                this.myOrder = (int)(((this.myAttenuation - 7.95f) / (this.myBand * 14.36f / this.myFS) + 1.0f) * 2.0f) - 1;

            float[] window = new float[(this.myOrder / 2) + 1];  // Оконная функция
            float[] coEff = new float[this.myOrder + 1];  // Коэффициенты фильтра
            float ps;
            float pe;
            const float PI = (float)System.Math.PI;
            int o2 = this.myOrder / 2;

            // Переключаемся в зависимости от алгоритма
            switch (alg)
            {
                case Algorithm.Kaiser:
                    // Оконная функция Кайзера
                    for (int i = 1; i <= o2; i++)
                    {
                        window[i] = Bessel(this.myAlpha * (float)System.Math.Sqrt(1.0f - (float)System.Math.Pow((float)i / o2, 2))) / Bessel(this.myAlpha);
                    }

                    // Ослабление в полосе подавления и полоса перехода должны быть установлены пользователем
                    break;

                case Algorithm.Hann:
                    // Оконная функция Ханна
                    for (int i = 1; i <= o2; i++)
                    {
                        window[i] = 0.5f + 0.5f * (float)System.Math.Cos((PI / (o2 + 1)) * i);
                    }

                    // Устанавливаем минимальное ослабление в полосе подавления
                    this.StopBandAttenuation = 44.0f;

                    // Устанавливаем полосу перехода
                    this.TransitionBand = 6.22f * this.myFS / this.myOrder;
                    break;

                case Algorithm.Hamming:
                    // Оконная функция Хэмминга
                    for (int i = 1; i <= o2; i++)
                    {
                        window[i] = 0.54f + 0.46f * (float)System.Math.Cos((PI / o2) * i);
                    }

                    // Устанавливаем минимальное ослабление в полосе подавления
                    this.StopBandAttenuation = 53.0f;

                    // Устанавливаем полосу перехода
                    this.TransitionBand = 6.64f * this.myFS / this.myOrder;
                    break;

                case Algorithm.Blackman:
                    // Оконная функция Блэкмана
                    for (int i = 1; i <= o2; i++)
                    {
                        window[i] = 0.42f + 0.5f * (float)Math.Cos((PI / o2) * i) + 0.08f * (float)Math.Cos(2.0f * (PI / o2) * i);
                    }

                    // Устанавливаем минимальное ослабление в полосе подавления
                    this.StopBandAttenuation = 74.0f;

                    // Устанавливаем полосу перехода
                    this.TransitionBand = 11.13f * this.myFS / this.myOrder;
                    break;

                case Algorithm.Rectangular:
                    // Прямоугольная оконная функция
                    for (int i = 1; i <= o2; i++)
                    {
                        window[i] = 1.0f;
                    }

                    // Устанавливаем минимальное ослабление в полосе подавления
                    this.StopBandAttenuation = 21.0f;

                    // Устанавливаем полосу перехода
                    this.TransitionBand = 1.84f * this.myFS / this.myOrder;
                    break;

                default:
                    // Обнуляем все значения, если ничего не было установлено (ошибка)
                    for (int i = 1; i <= o2; i++)
                    {
                        window[i] = 0.0f;
                    }
                    break;
            }

            // Переключаемся в зависимости от типа фильтра
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

            // Устанавливаем первое значение коэффициента
            coEff[0] = pe / PI;

            // Вычисляем остальные коэффициенты
            for (int i = 1; i <= o2; i++)
            {
                coEff[i] = window[i] * (float)System.Math.Sin(i * pe) * (float)System.Math.Cos(i * ps) / (i * PI);
            }

            // Смещаем импульс
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
