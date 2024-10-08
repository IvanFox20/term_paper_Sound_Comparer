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

        private WaveFormat format; // Формат волны
        private short[] samples; // Массив семплов звука
        short[] a = new short[256]; // Массив для преобразования 8-битных семплов в 16-битные
        private static int fftPoint = 512; // Количество точек для FFT
        private int framelen = 512; // Длина одного кадра (в семплах)
        private float[] A; // Амплитуда (в децибелах), каждый массив хранит 512 элементов (1 отрезок ~232.2 мс)
        private string filename = ""; // Имя файла WAV
        private int nbframe = 0; // Количество кадров для анализа
        private FIRFilters firFilter; // Объект FIR-фильтра
        private int comparedFrame = 0; // Счетчик сравненных кадров
        private string missData = ""; // Переменная для хранения недостающих данных
        private const int MAX_PROGRESS = 500; // Максимальное значение для индикатора прогресса

        #endregion // Members


        #region Properties

        /// <summary>Возвращает информацию заголовка формата.</summary>
        public WaveFormat Format
        {
            get { return format; }
        }

        /// <summary>Возвращает пользовательский элемент управления для аудиографики.</summary>
        public WaveControl GraphicControl
        {
            get;
            set;
        }

        /// <summary>Возвращает элемент управления индикатором прогресса.</summary>
        public ProgressBar ProgressBar
        {
            get;
            set;
        }

        /// <summary>Возвращает количество семплов.</summary>
        public int Count
        {
            get { return samples.Length; }
        }

        /// <summary>Возвращает амплитуду.</summary>
        public float[] Amplitude
        {
            get { return A; }
        }

        /// <summary>Возвращает семпл по заданной позиции.</summary>
        public short this[int indexer]
        {
            get { return samples[indexer]; }
            set { samples[indexer] = value; }
        }

        /// <summary>Возвращает массив семплов волны.</summary>
        public short[] Samples
        {
            get { return samples; }
        }

        /// <summary>Возвращает количество кадров для анализа и отображения.</summary>
        public int NbFrame
        {
            get { return nbframe; }
        }

        /// <summary>Возвращает или задает имя файла.</summary>
        public string Filename
        {
            get { return filename; }
            set { filename = value; }
        }

        /// <summary>Задает количество точек FFT.</summary>
        public int FFTPoint
        {
            set { fftPoint = value; }
        }

        /// <summary>Задает длину кадра.</summary>
        public int Frame
        {
            set { framelen = value; }
        }

        #endregion // Properties


        #region Constructors

        /// <summary>Конструктор.</summary>
        /// <param name="format">Информация заголовка формата.</param>
        /// <param name="samples">Сэмплы звука WAV.</param>
        public WaveSound(WaveFormat format, short[] samples)
        {
            // Заполнение массива 'a' значениями от -128 до 127
            for (short i = -128; i < 0; i++)
                a[128 + i] = i;
            for (short i = 0; i < 128; i++)
                a[128 + i] = i;

            firFilter = new FIRFilters(); // Инициализация FIR-фильтра
            this.format = format; // Установка формата
            this.samples = samples; // Установка сэмплов
        }

        /// <summary>Конструктор.</summary>
        /// <param name="fileName">Путь к WAV файлу.</param>
        /// <param name="graphicControl">Графическая панель, на которой будут отображаться данные WAV.</param>
        /// <param name="progressBar">Индикатор прогресса для отображения хода загрузки и чтения WAV файла.</param>
        public WaveSound(string fileName, WaveControl graphicControl, ProgressBar progressBar)
        {
            // Инициализация настроек пользовательского интерфейса (графический контрол и индикатор прогресса)
            GraphicControl = graphicControl; // Установка графического контроллера
            ProgressBar = progressBar; // Установка индикатора прогресса
            ProgressBar.Maximum = MAX_PROGRESS; // Установка максимального значения индикатора прогресса
            ProgressBar.Value = 0; // Начальное значение индикатора прогресса
            ProgressBar.Step = 1; // Шаг индикатора прогресса

            // Заполнение массива 'a' значениями от -128 до 127
            for (short i = -128; i < 0; i++)
                a[128 + i] = i;
            for (short i = 0; i < 128; i++)
                a[128 + i] = i;

            firFilter = new FIRFilters(); // Инициализация FIR-фильтра
            filename = fileName; // Установка имени файла
            samples = new short[4096]; // Инициализация массива сэмплов размером 4096
        }

        #endregion // Constructors


        #region Functions to process WAV

        /// <summary>Читает WAV-файл с использованием фонового рабочего процесса, поскольку необходимо сообщать о прогрессе.</summary>
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

        /// <summary>Читает WAV-файл и продолжает сообщать о прогрессе.</summary>
        /// <param name="sender">Элемент управления, для которого выполняется действие</param> 
        /// <param name="e">Аргументы события</param>
        /// <returns></returns>

        public void bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            // Чтение WAV-файла и запись FFT-файла
            using (BinaryReader reader = new BinaryReader(new FileStream(filename, FileMode.Open)))
            using (BinaryWriter fft = new BinaryWriter(File.Open(filename + ".fft.dat", FileMode.Create)))
            {
                // Чтение заголовка WAV-файла
                this.format = ReadHeader(reader);

                // Проверка, чтобы количество бит на семпл было либо 8, либо 16
                if (format.BitsPerSample != 8 && format.BitsPerSample != 16)
                {
                    System.Windows.Forms.MessageBox.Show("Звук имеет " + format.BitsPerSample + " бит на семпл. Выберите звук с 8 или 16 битами на семпл.");
                    return;
                }

                int bytesPerSample = format.BitsPerSample / 8;  // Определение количества байт на семпл
                int dataLength = reader.ReadInt32();            // Длина данных
                int countSamples = dataLength / bytesPerSample; // Количество семплов

                int di = 5;
                int b = reader.ReadByte(); // Чтение одного байта
                int adv = 1;               // Счётчик смещения

                // Пропуск байтов, если они попадают в диапазон от 120 до 140
                while (b > 120 && b < 140)
                {
                    reader.ReadBytes(di - 1); // Пропуск байтов
                    b = reader.ReadByte();
                    adv += di;
                }
                countSamples = countSamples - adv; // Коррекция количества семплов с учётом смещения

                float[] channelSamples16 = new float[framelen]; // Буфер данных на один кадр

                // Определение количества шагов для выборки данных
                int si = countSamples / (samples.Length * di);
                if (countSamples % (si * (samples.Length * di)) != 0)
                {
                    si++;
                }

                int overlap = (int)(0 * framelen); // Параметр наложения кадров

                float[] sampelOverlap = null;
                if (overlap > 0)
                    sampelOverlap = new float[overlap]; // Буфер для наложенных данных
                int nbsi = (framelen - overlap) / si;

                // Увеличение количества шагов выборки, если шагов недостаточно
                while (nbsi == 0)
                {
                    si = si - framelen;
                    nbsi = (framelen - overlap) / si;
                }
                if ((framelen - overlap) % (nbsi * si) != 0)
                {
                    nbsi++;
                }
                countSamples = countSamples - (overlap * di); // Коррекция количества семплов с учётом наложения
                nbframe = countSamples / ((framelen - overlap) * di); // Количество кадров

                // Коррекция количества кадров, если есть остаток
                if (countSamples % ((framelen - overlap) * di) != 0)
                {
                    nbframe += 1;
                }

                A = new float[fftPoint / 2]; // Буфер для результата FFT одного кадра
                int channelSamplesIndex = 0; // Индекс для работы с буфером семплов
                int endloop = 0;
                byte channelSample8;
                Int16 sample16;
                int endfft = 0;

                // Настройка фильтра (частотного диапазона)
                firFilter.FreqFrom = 500;
                firFilter.FreqTo = 4000;
                firFilter.CalculateCoefficients(FFT.Algorithm.Blackman, FFT.FilterType.BandPass);

                // Чтение наложенных семплов
                channelSamplesIndex = 0;
                if (format.BitsPerSample == 8)
                {
                    for (int sampleIndex = 0; sampleIndex < overlap; sampleIndex++)
                    {
                        channelSample8 = reader.ReadByte(); // Чтение 8-битного семпла
                        sampelOverlap[channelSamplesIndex] = (short)((a[(short)(channelSample8)]) << 8); // Преобразование в 16-битный формат
                        channelSamplesIndex++;
                        reader.ReadBytes(di - 1);
                        sampleIndex += di - 1;
                    }
                }
                else
                {
                    for (int sampleIndex = 0; sampleIndex < overlap; sampleIndex++)
                    {
                        sample16 = reader.ReadInt16(); // Чтение 16-битного семпла
                        sampelOverlap[channelSamplesIndex] = reader.ReadInt16();
                        channelSamplesIndex++;
                        reader.ReadBytes(2 * (di - 1));
                        sampleIndex += di - 1;
                    }
                }

                // Установка прогресса
                int progressInPercentage = 0;
                int factor = 100;
                float progressValue = 0;
                float dprogressvalue = (float)factor / (float)nbframe; // Шаг для обновления прогресса
                while ((int)dprogressvalue >= 100 || (int)dprogressvalue == 0)
                {
                    factor *= 10;
                    dprogressvalue = factor / nbframe;
                }

                for (int i = 0; i < nbframe; i++)
                {
                    // Копирование наложенных данных в буфер
                    for (int sampleIndex = 0; sampleIndex < overlap; sampleIndex++)
                    {
                        channelSamples16[sampleIndex] = sampelOverlap[sampleIndex];
                    }
                    endloop = di * (channelSamples16.Length - overlap);

                    // Если это последний кадр, корректируем количество данных
                    if (i == (nbframe - 1))
                    {
                        endloop = countSamples % endloop;
                    }
                    channelSamplesIndex = overlap;

                    #region Чтение данных

                    if (format.BitsPerSample == 8)
                    {
                        try
                        {
                            // Чтение данных для 8-битного формата
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
                            // Чтение данных для 16-битного формата
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

                    #endregion // Чтение данных

                    // Подготовка данных для отображения
                    for (int j = 0; j < nbsi && (j + i * nbsi) < samples.Length; j++)
                    {
                        samples[j + i * nbsi] = (short)channelSamples16[j * si];
                    }

                    if (i != (nbframe - 1))
                    {
                        // Обновление данных наложения
                        for (int sampleIndex = framelen - overlap; sampleIndex < framelen; sampleIndex++)
                        {
                            sampelOverlap[sampleIndex - (framelen - overlap)] = channelSamples16[sampleIndex];
                        }
                    }

                    // Применение фильтра
                    FilterCalculation(ref channelSamples16, FFT.FilterType.BandPass);

                    // Применение FFT
                    ApplyFFT(channelSamples16);

                    endfft = fftPoint / 2;

                    // Ограничение данных для записи в файл FFT
                    if (channelSamplesIndex != 0)
                    {
                        if (endfft > channelSamplesIndex / 2)
                            endfft = channelSamplesIndex / 2;
                    }
                    for (int j = 1; j < endfft; j++)
                    {
                        fft.Write((Int16)A[j]);
                    }
                    fft.Write(-9999); // Индикатор конца данных FFT

                    // Обновление прогресса
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


        /// <summary>Завершает работу индикатора прогресса и обновляет графический элемент управления после завершения чтения WAV-файла.</summary>
        /// <param name="sender">Элемент управления, для которого выполняется действие</param> 
        /// <param name="e">Аргументы события</param>
        /// <returns></returns>

        void bgw_WorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ProgressBar.Value = MAX_PROGRESS;
            GraphicControl.Refresh();
        }

        /// <summary>Обновляет индикатор прогресса при срабатывании события прогресса.</summary>
        /// <param name="sender">Элемент управления, для которого выполняется действие</param> 
        /// <param name="e">Аргументы события</param>
        /// <returns></returns>

        void bgw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBar.PerformStep();
        }

        /// <summary>Читает блок из четырех байт из WAV-файла.</summary>
        /// <param name="reader">Читатель для WAV-файла.</param>
        /// <returns>Четыре символа.</returns>

        private string ReadChunk(BinaryReader reader)
        {
            byte[] ch = new byte[4];
            reader.Read(ch, 0, ch.Length);
            return System.Text.Encoding.ASCII.GetString(ch,0,4);
        }

        /// <summary>
        /// Читает заголовок из WAV-файла и перемещает
        /// позицию читателя к началу блока данных.
        /// </summary>
        /// <param name="reader">Читатель для WAV-файла.</param>
        /// <returns>Формат WAV-файла.</returns>

        private WaveFormat ReadHeader(BinaryReader reader)
        {
            reader.BaseStream.Position = 0;
            if (ReadChunk(reader) != "RIFF")
                throw new Exception("Invalid file format");

            reader.ReadUInt32(); // Длина файла минус 8 байт, для RIFF описания, они не используются
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

        /// <summary>Сравнивает текущий звуковой файл с другим файлом, используя алгоритм БПФ.</summary>
        /// <param name="wf">Звуковой файл, с которым происходит сравнение</param> 
        /// <returns>Процент сходства</returns>
        public float Compare(WaveSound wf)
        {
            int nbcorrect = 0; // количество совпадающих коэффициентов
            int i = 0;
            bool endfile = false; // флаг окончания файла
            int a = 0, b = 0; // временные переменные для хранения данных из файлов
            float[] frames = new float[nbframe > wf.nbframe ? nbframe : wf.nbframe]; // массив для хранения сходства по кадрам
            comparedFrame = nbframe < wf.nbframe ? nbframe : wf.nbframe; // количество кадров для сравнения
            short[] MissPlay = new short[comparedFrame]; // массив для хранения номеров кадров с расхождениями
            int step = 0;
            if (ProgressBar.Value != MAX_PROGRESS)
                step = (MAX_PROGRESS - ProgressBar.Value) / comparedFrame;

            // Открытие файлов с данными БПФ
            using (BinaryReader f1 = new BinaryReader(File.Open(filename + ".fft.dat", FileMode.Open)))
            using (BinaryReader f2 = new BinaryReader(File.Open(wf.filename + ".fft.dat", FileMode.Open)))
            using (StreamWriter sw2 = new StreamWriter(wf.filename + ".fft.txt"))
            {
                while (!endfile)
                {
                    try
                    {
                        a = f1.ReadInt16(); // читаем данные из первого файла
                        b = f2.ReadInt16(); // читаем данные из второго файла
                        if (a == -9999 || b == -9999)
                        {
                            // Рассчитываем процент совпадений для одного кадра
                            frames[i] = (float)nbcorrect / (fftPoint / 2);
                            ProgressBar.Value += step; // обновляем прогресс
                            i++;
                            nbcorrect = 0;
                        }
                        else
                        {
                            // Сравниваем коэффициенты, если они отличаются не более чем на 2, то считаем их одинаковыми
                            if (Math.Abs(a - b) <= 2)
                            {
                                nbcorrect++;
                            }
                        }
                        sw2.Write(a); // записываем данные для дальнейшего анализа
                        sw2.WriteLine("," + b);
                    }
                    catch
                    {
                        endfile = true; // если достигли конца файла
                        f1.Close();
                        f2.Close();
                        sw2.Close();
                    }
                }

                nbcorrect = 0;
                int idx = 0;
                int len = comparedFrame;
                comparedFrame = 0;

                // Анализируем кадры и определяем, насколько они схожи
                for (i = 0; i < len; i++)
                {
                    nbcorrect += frames[i] >= 0.7 ? 1 : 0; // если кадр схож более чем на 70%, увеличиваем счетчик
                    if (frames[i] < 0.7)
                    {
                        MissPlay[idx] = (short)i; // записываем номера кадров с расхождениями
                        idx++;
                        comparedFrame++;
                    }
                }

                f1.Close();
                f2.Close();
                sw2.Close();

                // Записываем номера кадров, где были расхождения
                for (i = 0; i < comparedFrame; i++)
                    missData = missData + MissPlay[i] + ";";
            }

            return (float)(nbcorrect * 100 / frames.Length); // возвращаем процент сходства
        }

        /// <summary>Получить несколько кадров данных из файла</summary>
        List<int> GetNextFrames(int count, BinaryReader reader)
        {
            List<int> frames = new List<int>();
            for (int i = 0; i < count; i++)
            {
                frames.AddRange(GetNextFrame(reader)); // считываем по кадрам
            }
            return frames;
        }

        /// <summary>Получить один кадр данных из файла</summary>
        List<int> GetNextFrame(BinaryReader reader)
        {
            List<int> frame = new List<int>();
            int coef = reader.ReadInt16(); // читаем коэффициенты
            while (coef != -9999 && reader.BaseStream.Position < reader.BaseStream.Length - 1)
            {
                frame.Add(coef); // добавляем коэффициенты в список
                coef = reader.ReadInt16();
            }
            return frame;
        }

        /// <summary>Применение алгоритма БПФ</summary>
        /// <param name="data">Данные, которые нужно обработать через БПФ</param> 
        public void ApplyFFT(float[] data)
        {
            float[] a2 = new float[fftPoint]; // временные массивы для хранения реальной и мнимой частей
            float[] a1 = new float[fftPoint];
            int loop = data.Length / fftPoint; // количество итераций для обработки всех данных
            if ((loop * fftPoint) < data.Length)
                loop += 1;
            int inloop, k = 0, stop, t;
            int log2n = ilog2(fftPoint); // логарифм от длины БПФ
            float maxA = float.MinValue, minA = float.MaxValue;
            float refA = 0;

            for (int i = 0; i < loop; i++)
            {
                k = 0;
                inloop = i * fftPoint;
                stop = inloop + fftPoint;
                if (stop > data.Length)
                    stop = data.Length;

                // Перестановка данных и разбиение на реальные и мнимые части
                for (int j = inloop; j < stop; j++)
                {
                    t = bitrev(k, log2n); // битовая инверсия
                    a1[t] = data[j];
                    a2[t] = 0.0f;
                    k++;
                }

                // Дополняем оставшиеся данные нулями
                if (stop % fftPoint != 0)
                {
                    for (int l = stop % fftPoint; l < fftPoint; l++)
                    {
                        t = bitrev(l, log2n);
                        a1[t] = 0f;
                        a2[t] = 0.0f;
                    }
                }

                fft(log2n, ref a2, ref a1, fftPoint, 1); // Применяем БПФ

                k = 0;
                a1[0] = 0; a2[0] = 0; // обнуляем нулевые коэффициенты
                maxA = float.MinValue;
                minA = float.MaxValue;

                // Рассчитываем амплитуды
                for (int v = inloop / 2; v < stop / 2; v++)
                {
                    A[v] = (a1[k] * a1[k]) + (a2[k] * a2[k]);
                    maxA = Math.Max(A[v], maxA);
                    minA = Math.Min(A[v], minA);
                    k++;
                }

                refA = maxA - minA; // расчет контрольного значения

                // Преобразуем амплитуды в децибелы
                for (int v = inloop / 2 + 1; v < stop / 2; v++)
                    A[v] = 10f * (float)Math.Log10(A[v] / refA);
                for (int v = stop / 2; v < fftPoint / 2; v++)
                    A[v] = 0;
            }
        }

        /// <summary>Выполнение БПФ</summary>
        void fft(int log2n, ref float[] a2, ref float[] a1, int n, int sgn)
        {
            int i, j, k, k2, s, m;
            float wm1, wm2, w1, w2, t1, t2, u1, u2;

            // Проходим по стадиям БПФ
            for (s = 1; s <= log2n; s++)
            {
                m = 1 << s; // m = 2^s
                wm1 = (float)Math.Cos(sgn * 2 * Math.PI / m); // расчет вращающих множителей
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

                    // Обновляем вращающие множители
                    t1 = w1 * wm1 - w2 * wm2;
                    w2 = w1 * wm2 + w2 * wm1;
                    w1 = t1;
                }
            }

            // Переворот последней стадии
            for (i = 1; i < n / 2; i++)
            {
                t1 = a1[i];
                a1[i] = a1[n - i];
                a1[n - i] = t1;
                t2 = a2[i];
                a2[i] = a2[n - i];
                a2[n - i] = t2;
            }

            // Если обратное преобразование, делим на n
            if (sgn == -1)
            {
                for (i = 0; i < n; i++)
                {
                    a1[i] /= (float)n;
                    a2[i] /= (float)n;
                }
            }
        }

        /// <summary>Логарифм по основанию 2</summary>
        private int ilog2(int n)
        {
            int i;
            for (i = 8 * sizeof(int) - 1; i >= 0 && ((1 << i) & n) == 0; i--) ;
            return i;
        }

        /// <summary>Инверсия битов в числе "a" для диапазона 0 до k-1</summary>
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
