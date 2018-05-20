﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace EarTrumpet.Services
{
    public class ThemeService : ViewModels.BindableBase
    {
        public interface ResolvableThemeBrush
        {
            Color Resolve(ThemeResolveData data);
        }

        public class ThemeResolveData
        {
            public bool IsHighContrast => SystemParameters.HighContrast;
            public bool IsTransparencyEnabled => UserSystemPreferencesService.IsTransparencyEnabled;
            public bool IsLightTheme => UserSystemPreferencesService.IsLightTheme;
            public bool UseAccentColor => UserSystemPreferencesService.UseAccentColor;
            public Color LookupThemeColor(string color) => AccentColorService.GetColorByTypeName(color);
        }

        public event Action ThemeChanged;

        public bool AnimationsEnabled => SystemParameters.MenuAnimation;
        public bool IsLightTheme => UserSystemPreferencesService.IsLightTheme;

        private Dictionary<string, ResolvableThemeBrush> _themeData;

        public void SetTheme(Dictionary<string, ThemeService.ResolvableThemeBrush> data)
        {
            _themeData = data;
        }

        public void RegisterForThemeChanges(IntPtr hwnd)
        {
            var src = HwndSource.FromHwnd(hwnd);
            src.AddHook(WndProc);

            RebuildTheme();
        }

        private void RebuildTheme()
        {
            var resolveData = new ThemeResolveData();
            var newDictionary = new ResourceDictionary();
            foreach (var themeEntry in _themeData)
            {
                newDictionary[themeEntry.Key] = new SolidColorBrush(themeEntry.Value.Resolve(resolveData));
            }

            Application.Current.Resources.MergedDictionaries.RemoveAt(0);
            Application.Current.Resources.MergedDictionaries.Insert(0, newDictionary);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_DWMCOLORIZATIONCOLORCHANGED = 0x320;
            const int WM_DWMCOMPOSITIONCHANGED = 0x31E;
            const int WM_THEMECHANGED = 0x031A;
            const int WM_SETTINGCHANGE = 0x001A;

            switch (msg)
            {
                case WM_DWMCOLORIZATIONCOLORCHANGED:
                case WM_DWMCOMPOSITIONCHANGED:
                case WM_THEMECHANGED:
                    OnThemeColorsChanged();
                    break;
                case WM_SETTINGCHANGE:
                    var settingChanged = Marshal.PtrToStringUni(lParam);
                    if (settingChanged == "ImmersiveColorSet")
                    {
                        OnThemeColorsChanged();
                    }
                    else if (settingChanged == "WindowMetrics")
                    {
                        RaisePropertyChanged(nameof(AnimationsEnabled));
                    }
                    break;
                default:
                    break;
            }
            return IntPtr.Zero;
        }

        private void OnThemeColorsChanged()
        {
            Debug.WriteLine("Theme changed");

            RebuildTheme();

            ThemeChanged?.Invoke();

            RaisePropertyChanged(nameof(IsLightTheme));
        }
    }
}