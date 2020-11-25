using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NAudio;

public class MusicProcessor
{
    public AudioClip musicClip;

    public float[,] frequencySamplesStore { get; private set; }
    public float[,] frequencyAvarageStore { get; private set; }
    public float[,] frequenciesCuttOff { get; private set; }
    public bool[,] frequenciesPeaks { get; private set; }
    public int[] frequenciesPeaksCount { get; private set; }

    public float[] spectralFlux { get; private set; }
    public float[] spectralFluxAvarage { get; private set; }
    public float[] fluxCuttOff { get; private set; }
    public bool[] fluxPeaks { get; private set; }
    public int[] fluxPeaksInWindow { get; private set; }

    private int _numberOfTimeSamples = 1024;

    public bool isProceed = false;

    private Dictionary<int, int> _frequencesInSubbund = new Dictionary<int, int>();

    private readonly int NUMBER_OF_FREQUENCY_SUBBUNDS = 128;
    private readonly int AVARAGE_ENERGY_WINDOW_SIZE = 10;
    private readonly float AVARAGE_ENERGY_MULTIPLIER = 4f;
    private readonly int FLUX_PEAKS_WINDOW_SIZE = 86; //Половина от 4 секунд, если 1 секунда -- это 43
    private readonly int BPM_SCALING = 24;

    public void CreateSpectrumData()
    {
        _numberOfTimeSamples = musicClip.frequency > 70000 ? 2048 : 1024;
        UpdateNumberOfFrequencesInSubbund(_numberOfTimeSamples);
        int numberOfSamplesInSubbund = musicClip.samples % _numberOfTimeSamples == 0 ? musicClip.samples / _numberOfTimeSamples : (musicClip.samples / _numberOfTimeSamples) + 1;

        frequencySamplesStore = new float[NUMBER_OF_FREQUENCY_SUBBUNDS, numberOfSamplesInSubbund];
        spectralFlux = new float[numberOfSamplesInSubbund];

        float[] timeSamples = musicClip.channels == 2 ? new float[2 * musicClip.samples] : new float[musicClip.samples];
        musicClip.GetData(timeSamples, 0);

        NAudio.Dsp.Complex[] spectrumSamples = new NAudio.Dsp.Complex[_numberOfTimeSamples];

        for (int i = 0; i < numberOfSamplesInSubbund; i++)
        {
            int samplesOffset = musicClip.channels == 2 ? 2 * i * _numberOfTimeSamples : i * _numberOfTimeSamples;
            for (int j = 0; j < _numberOfTimeSamples; j++)
            {
                if (musicClip.channels == 2)
                {
                    if ((i == numberOfSamplesInSubbund - 1) && (samplesOffset + 2 * j >= musicClip.samples))
                    {
                        spectrumSamples[j].X = 0;
                        spectrumSamples[j].Y = 0;
                    }
                    else
                    {
                        spectrumSamples[j].X = (timeSamples[samplesOffset + 2 * j] + timeSamples[samplesOffset + 2 * j + 1]) / 2f * (float)NAudio.Dsp.FastFourierTransform.HannWindow(j, _numberOfTimeSamples);
                        spectrumSamples[j].Y = 0;
                    }
                }
                else
                {
                    if ((i == numberOfSamplesInSubbund - 1) && (samplesOffset + j >= musicClip.samples))
                    {
                        spectrumSamples[j].X = 0;
                        spectrumSamples[j].Y = 0;
                    }
                    else
                    {
                        spectrumSamples[j].X = timeSamples[samplesOffset + j] * (float)NAudio.Dsp.FastFourierTransform.HannWindow(j, _numberOfTimeSamples);
                        spectrumSamples[j].Y = 0;
                    }
                }
            }

            NAudio.Dsp.FastFourierTransform.FFT(true, _numberOfTimeSamples == 1024 ? 10 : 11, spectrumSamples);
            int subbundOffset = 0;
            float flux = 0;

            for (int j = 0; j < NUMBER_OF_FREQUENCY_SUBBUNDS; j++)
            {
                float subbundEnergy = 0;
                int numberOfFreq = 0;
                _frequencesInSubbund.TryGetValue(j, out numberOfFreq);

                for (int k = 0; k < numberOfFreq; k++)
                {
                    subbundEnergy += spectrumSamples[subbundOffset + k].X * spectrumSamples[subbundOffset + k].X + spectrumSamples[subbundOffset + k].Y * spectrumSamples[subbundOffset + k].Y;
                }
                subbundOffset += numberOfFreq;

                subbundEnergy *= (float)(2 * numberOfFreq) / _numberOfTimeSamples;
                frequencySamplesStore[j, i] = subbundEnergy;
                flux += i == 0 ? subbundEnergy : subbundEnergy - frequencySamplesStore[j, i - 1];
            }
            spectralFlux[i] = flux < 0 ? 0 : flux;
        }

        spectralFluxAvarage = new float[numberOfSamplesInSubbund];
        frequencyAvarageStore = new float[NUMBER_OF_FREQUENCY_SUBBUNDS, numberOfSamplesInSubbund];
        fluxCuttOff = new float[numberOfSamplesInSubbund];
        frequenciesCuttOff = new float[NUMBER_OF_FREQUENCY_SUBBUNDS, numberOfSamplesInSubbund];
        fluxPeaks = new bool[numberOfSamplesInSubbund];
        frequenciesPeaks = new bool[NUMBER_OF_FREQUENCY_SUBBUNDS, numberOfSamplesInSubbund];

        for (int i = 0; i < numberOfSamplesInSubbund; i++)
        {
            int start = Mathf.Max(0, i - AVARAGE_ENERGY_WINDOW_SIZE);
            int end = Mathf.Min(i + AVARAGE_ENERGY_WINDOW_SIZE + 1, numberOfSamplesInSubbund);

            for (int n = 0; n < NUMBER_OF_FREQUENCY_SUBBUNDS + 1; n++)
            {
                if (n != NUMBER_OF_FREQUENCY_SUBBUNDS)
                {
                    if (i == 0 || i == numberOfSamplesInSubbund - 1)
                        frequenciesPeaks[n, i] = false;
                    float avEnergy = 0;
                    for (int j = start; j < end; j++)
                        avEnergy += frequencySamplesStore[n, j];
                    frequencyAvarageStore[n, i] = avEnergy * AVARAGE_ENERGY_MULTIPLIER / (end - start);
                    frequenciesCuttOff[n, i] = frequencySamplesStore[n, i] > frequencyAvarageStore[n, i] ? frequencySamplesStore[n, i] - frequencyAvarageStore[n, i] : 0;
                    if (i > 1) frequenciesPeaks[n, i - 1] = (frequenciesCuttOff[n, i - 1] > frequenciesCuttOff[n, i - 2]) && (frequenciesCuttOff[n, i - 1] > frequenciesCuttOff[n, i]) ? true : false;
                }
                else
                {
                    if (i == 0 || i == numberOfSamplesInSubbund - 1)
                        fluxPeaks[i] = false;
                    float avEnergy = 0;
                    for (int j = start; j < end; j++)
                        avEnergy += spectralFlux[j];
                    spectralFluxAvarage[i] = avEnergy * AVARAGE_ENERGY_MULTIPLIER / (end - start);
                    fluxCuttOff[i] = spectralFlux[i] > spectralFluxAvarage[i] ? spectralFlux[i] - spectralFluxAvarage[i] : 0;
                    if (i > 1) fluxPeaks[i - 1] = (fluxCuttOff[i - 1] > fluxCuttOff[i - 2]) && (fluxCuttOff[i - 1] > fluxCuttOff[i]) ? true : false;
                }   
            }
        }

        fluxPeaksInWindow = new int[numberOfSamplesInSubbund];
        for (int i = 0; i < numberOfSamplesInSubbund; i++)
        {
            float peaks = 0;
            int start = Mathf.Max(0, i - FLUX_PEAKS_WINDOW_SIZE);
            int end = Mathf.Min(i + FLUX_PEAKS_WINDOW_SIZE + 1, numberOfSamplesInSubbund);
            for (int j = start; j < end; j++)
                if (fluxPeaks[j]) peaks++;
            peaks *= 60f * 43f / (end - start);
            fluxPeaksInWindow[i] = (int)peaks;
        }

        frequenciesPeaksCount = new int[NUMBER_OF_FREQUENCY_SUBBUNDS];
        for (int i = 0; i < NUMBER_OF_FREQUENCY_SUBBUNDS; i++)
        {
            int numberOfPeaks = 0;
            for (int j = 0; j < numberOfSamplesInSubbund; j++)
            {
                if (frequenciesPeaks[i, j] == true) numberOfPeaks++;
            }
            frequenciesPeaksCount[i] += numberOfPeaks;
        }

        if (!isProceed)
            isProceed = true;
    }

    private void UpdateNumberOfFrequencesInSubbund(int numberOfTimeSamples)
    {
        _frequencesInSubbund.Clear();
        if (numberOfTimeSamples == 1024)
            for (int i = 0; i < NUMBER_OF_FREQUENCY_SUBBUNDS; i++)
                _frequencesInSubbund.Add(i, (int)(0.0551 * i + 1));
        else
            for (int i = 0; i < NUMBER_OF_FREQUENCY_SUBBUNDS; i++)
                _frequencesInSubbund.Add(i, (int)(0.1179 * i + 1));
    }

    public float[] GetLenghtFromBPM()
    {
        float[] peaks = new float[fluxPeaksInWindow.Length % BPM_SCALING == 0 ? fluxPeaksInWindow.Length / BPM_SCALING : fluxPeaksInWindow.Length / BPM_SCALING + 1];
        for (int i = 0; i < peaks.Length; i++)
        {
            float BPM = Mathf.Clamp(fluxPeaksInWindow[i * BPM_SCALING], 50, 300);
            peaks[i] = BPM * (-0.018f) + 5.9f; // 50 = 5, 300 = 0.5
        }
        return peaks;
    }
}
