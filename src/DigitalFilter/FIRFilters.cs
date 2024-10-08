using System;

namespace SoundComparer.DigitalFilter
{
    /// <summary>
    /// Вспомогательный класс, который реализует функции фильтрации. В настоящее время
    /// реализованы низкочастотный, высокочастотный и полосовой фильтры.
    /// </summary>
    class FIRFilters
    {
        #region Members

        /// <summary>
        /// Количество отсчетов такое же, как и количество точек
        /// </summary>
        private int mySamples;

        /// <summary>
        /// Массив коэффициентов оконной функции
        /// </summary>
        private float[] myCoeff;

        /// <summary>
        /// Массив, который содержит входные данные
        /// </summary>
        private float[] myInputSeries;

        /// <summary>
        /// Объект для выполнения алгоритма БПФ (быстрого преобразования Фурье)
        /// </summary>
        private FFT myFFT;

        /// <summary>
        /// Текущий выбранный алгоритм. Тип перечисления взят из объекта БПФ.
        /// </summary>
        public FFT.Algorithm CurrentAlgorithm;

        /// <summary>
        /// Текущий тип фильтра
        /// </summary>
        public FFT.FilterType CurrentFilter;

        private float myFreqFrom;   // Частота начала полосы пропускания
        private float myFreqTo;     // Частота конца полосы пропускания
        private float myAttenuation; // Ослабление в полосе подавления
        private float myBand;        // Полоса перехода
        private float myAlpha;       // Параметр альфа для оконной функции Кайзера
        private int myTaps;          // Количество отсчетов
        private int myOrder;         // Порядок фильтра (должен быть четным)

        #endregion // Members

        #region Properties

        /// <summary>
        /// Частота начала полосы пропускания, должна быть ниже частоты окончания.
        /// </summary>
        public float FreqFrom
        {
            get { return myFreqFrom; }
            set { myFreqFrom = value; }
        }

        /// <summary>
        /// Частота окончания полосы пропускания, должна быть выше частоты начала.
        /// </summary>
        public float FreqTo
        {
            get { return myFreqTo; }
            set { myFreqTo = value; }
        }

        /// <summary>
        /// Ослабление в полосе подавления
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
        /// Полоса перехода
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
        /// Параметр альфа для алгоритма Кайзера.
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
        /// Количество отсчетов (taps), которое используется. Это количество выборок, обрабатываемых за раз.
        /// </summary>
        public int Taps
        {
            get { return myTaps; }
            set { myTaps = value; }
        }

        /// <summary>
        /// Порядок фильтра. Должен быть четным числом.
        /// </summary>
        public int Order
        {
            get { return myOrder; }
            set
            {
                // Убедиться, что значение четное
                if ((value % 2) == 0)
                {
                    myOrder = value;
                    this.myFFT.Order = myOrder;
                }
                else
                    throw new ArgumentOutOfRangeException("Order", "Порядок фильтра должен быть четным числом.");
            }
        }

        #endregion // Properties

        #region Constructors

        /// <summary>
        /// Основной конструктор. Сбрасывает все настройки внутри объекта БПФ.
        /// </summary>
        public FIRFilters()
        {
            // Создаем новый объект для БПФ
            this.myFFT = new FFT();

            // Алгоритм по умолчанию — Кайзер
            this.CurrentAlgorithm = FFT.Algorithm.Kaiser;

            // Количество отсчетов по умолчанию равно 35
            this.myTaps = 35;
        }

        #endregion // Constructors

        #region Methods

        /// <summary>
        /// Выполняет низкочастотный фильтр. Выходной массив будет очищен перед записью в него данных.
        /// Если начальные и конечные частоты полосы пропускания равны 0, используются значения по умолчанию.
        /// </summary>
        public void LowPassFilter(ref float[] iseries)
        {
            // Если не заданы начальные и конечные частоты, используем диапазон по умолчанию 0 - 1000 Гц
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

            // Применяем фильтр к серии данных на основе сгенерированных коэффициентов
            Filter(ref iseries);
        }

        /// <summary>
        /// Вычисляет коэффициенты фильтра в зависимости от выбранного алгоритма и типа фильтра.
        /// </summary>
        public void CalculateCoefficients(FFT.Algorithm algorithm, FFT.FilterType filter)
        {
            // Генерируем коэффициенты
            this.CurrentAlgorithm = algorithm;
            this.CurrentFilter = filter;
            if (algorithm == FFT.Algorithm.Kaiser)
            {
                // Для оконной функции Кайзера необходимо задать ослабление
                this.StopBandAttenuation = 60;

                // Также требуется задать полосу перехода
                this.TransitionBand = 500;
            }

            // Если частоты не заданы, используем значения по умолчанию 1000 Гц
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

            // Генерируем коэффициенты
            this.myCoeff = this.myFFT.GenerateCoefficients(this.CurrentFilter, this.CurrentAlgorithm);
        }

        /// <summary>
        /// Выполняет высокочастотный фильтр. Выходной массив будет очищен перед записью в него данных.
        /// Если начальные и конечные частоты полосы пропускания равны 0, используются значения по умолчанию.
        /// </summary>
        public void HighPassFilter(ref float[] iseries)
        {
            // Если не заданы начальные и конечные частоты, используем диапазон по умолчанию 2000 - 4000 Гц
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

            // Применяем фильтр к серии данных на основе сгенерированных коэффициентов
            Filter(ref iseries);
        }

        /// <summary>
        /// Выполняет полосовой фильтр. Выходной массив будет очищен перед записью в него данных.
        /// Если начальные и конечные частоты полосы пропускания равны 0, используются значения по умолчанию.
        /// </summary>
        public void BandPassFilter(ref float[] iseries)
        {
            // Если не заданы начальные и конечные частоты, используем диапазон по умолчанию 1000 - 1000 Гц
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

            // Применяем фильтр к серии данных на основе сгенерированных коэффициентов
            Filter(ref iseries);
        }

        #endregion // Methods

        #region Initialization

        /// <summary>
        /// Инициализирует объект FIRFilters, устанавливая входные данные для использования фильтром.
        /// </summary>
        /// <param name="iseries">Входные данные, содержащие исходные данные</param>
        private void SetIOSeries(float[] iseries)
        {
            this.myInputSeries = iseries;

            // Количество отсчетов — это количество точек, содержащихся во входных данных
            this.mySamples = myInputSeries.Length;
        }

        #endregion // Initialization

        #region Filter

        /// <summary>
        /// Выполняет саму фильтрацию. Коэффициенты должны быть заранее сгенерированы вызывающей функцией,
        /// данная функция просто применяет их и добавляет точки в выходной массив.
        /// </summary>
        private void Filter(ref float[] iseries)
        {
            float[] x = new float[myTaps];  // Вспомогательный массив для хранения выборок
            float y;  // Переменная для накопления суммы результата фильтрации

            // Устанавливаем входную серию данных
            SetIOSeries(iseries);

            // Инициализируем массив x нулями
            for (int i = 1; i < myTaps; i++)
                x[i] = 0.0f;

            // Цикл по каждой точке входной серии данных
            for (int i = 0; i < iseries.Length; i++)
            {
                y = 0.0f;  // Инициализируем накопитель суммы

                // Получаем текущее значение данных
                x[0] = Convert.ToSingle(iseries[i]);

                // Цикл по коэффициентам фильтра и расчёт суммы произведений
                try
                {
                    for (int j = 0; j < myTaps; j++)
                        y = y + (x[j] * myCoeff[j]);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message + " Проверьте порядок фильтра.");
                    throw e;
                }

                // Сдвигаем значения в массиве x вправо
                for (int j = myTaps - 1; j > 0; j--)
                    x[j] = x[j - 1];

                // Записываем результат в исходный массив на текущее значение x
                iseries[i] = y;
            }
        }

        #endregion // Filter
    }
}
