﻿using GalaSoft.MvvmLight.Messaging;
using NAudio.Wave;
using SkyTimer.MVVM;
using SkyTimer.Properties;
using SkyTimer.Utils.Decoder;
using System;
using System.Diagnostics;
using System.Windows.Threading;

namespace SkyTimer.ViewModel
{
    public class TimerViewModel : ObservableObject
    {
        public TimerViewModel()
        {
            wave.DataAvailable += Wave_DataAvailable;
            wave.BufferMilliseconds = 100;
            wave.WaveFormat = new WaveFormat(8000, 8, 1);

            decoder.TimeUpdated += Decoder_TimeUpdated;
            //decoder.LostConnection += Decoder_LostConnection;

            timer.Tick += (sender, e) =>
            {
                Time = (int)watch.ElapsedMilliseconds;
            };

            wave.StartRecording();
        }

        private Stopwatch watch = new Stopwatch();
        private DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1) };

        private WaveIn wave = new WaveIn();
        private StackmatDecoder_8bit decoder = new StackmatDecoder_8bit(8000);

        private static int RedTimeOut = 500;

        private bool ssReady;
        private bool increasing;
        private int lastTime;

        private int time = 0;
        public int Time
        {
            get { return time; }
            set { Set(ref time, value); }
        }

        private StackmatStatus stackmatStatus = StackmatStatus.LostConnection;
        public StackmatStatus StackmatStatus
        {
            get { return stackmatStatus; }
            set
            {
                Set(ref stackmatStatus, value);
            }
        }


        private void Decoder_TimeUpdated(object sender, StackmatFrame e)
        {
            Time = e.Time;
            StackmatStatus = e.Status;

            if (Time == 0) ssReady = true;

            if (ssReady)
            {
                if (increasing && Time == lastTime)
                {
                    Messenger.Default.Send(Time);
                }

                increasing = Time > lastTime;
                lastTime = Time;
            }
        }

        private void Decoder_LostConnection(object sender, EventArgs e)
        {
            Time = 0;
            StackmatStatus = StackmatStatus.LostConnection;
        }

        private void Wave_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (Settings.Default.StackmatMode)
            {
                decoder.Decode(e.Buffer);
                if (watch.IsRunning) watch.Reset();
                if (timer.IsEnabled) timer.Stop();
            }
        }

        public void SpaceKeyDown()
        {
            if (!watch.IsRunning)
            {
                watch.Start();
            }
            if (timer.IsEnabled)
            {
                watch.Reset();
                timer.Stop();
                Messenger.Default.Send(Time);
            }
            else
            {
                if (watch.ElapsedMilliseconds < RedTimeOut) StackmatStatus = StackmatStatus.Red;
                else StackmatStatus = StackmatStatus.Green;
            }
        }

        public void SpaceKeyUp()
        {
            if (watch.IsRunning)
            {
                if (watch.ElapsedMilliseconds < RedTimeOut)
                {
                    StackmatStatus = StackmatStatus.Zero;
                    watch.Reset();
                }
                else
                {
                    timer.Start();
                    watch.Restart();
                    StackmatStatus = StackmatStatus.Timing;
                }
            }
        }
    }
}
