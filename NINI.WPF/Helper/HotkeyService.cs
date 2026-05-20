using NINI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace NINI.Helper
{
    /// <summary>
    /// 热键服务 - 管理热键的注册、注销、冲突检测和持久化
    /// </summary>
    public class HotkeyService : IDisposable
    {
        private readonly KeyboardHook _hook;
        private readonly Dictionary<string, int> _registeredIds = new Dictionary<string, int>();
        private readonly Dictionary<int, HotkeyItem> _idToHotkey = new Dictionary<int, HotkeyItem>();
        private string _settingsPath;
        private List<HotkeyItem> _hotkeys;

        /// <summary>
        /// 热键冲突时触发
        /// </summary>
        public event EventHandler<List<HotkeyItem>>? HotkeysConflicted;

        /// <summary>
        /// 热键配置变更时触发
        /// </summary>
        public event EventHandler? SettingsChanged;

        public HotkeyService()
        {
            _hook = new KeyboardHook();
            _settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NINI",
                "hotkeys.json"
            );
            _hotkeys = new List<HotkeyItem>();
        }

        // 常规设置属性
        public bool AutoStart { get; set; }
        public bool StartMinimized { get; set; }
        public bool RememberWindowPosition { get; set; } = true;
        public bool MinimizeToTray { get; set; } = true;
        public bool ShowNotifications { get; set; } = true;
        public bool EnableConflictDetection { get; set; } = true;

        /// <summary>
        /// 获取所有热键配置
        /// </summary>
        public IReadOnlyList<HotkeyItem> Hotkeys => _hotkeys;

        /// <summary>
        /// 获取 KeyboardHook 的 KeyPressed 事件
        /// </summary>
        public event EventHandler<KeyPressedEventArgs>? KeyPressed
        {
            add => _hook.KeyPressed += value;
            remove => _hook.KeyPressed -= value;
        }

        /// <summary>
        /// 加载默认热键配置
        /// </summary>
        private List<HotkeyItem> GetDefaultHotkeys()
        {
            return new List<HotkeyItem>
            {
                new HotkeyItem
                {
                    Id = "open_terminal",
                    Name = "打开终端",
                    Description = "打开命令行终端",
                    IsEnabled = true,
                    Modifier = (int)(ModifierKeys.Control | ModifierKeys.Alt),
                    KeyCode = (int)Keys.T
                },
                new HotkeyItem
                {
                    Id = "screen_saver",
                    Name = "屏幕保护",
                    Description = "启动屏幕保护",
                    IsEnabled = true,
                    Modifier = (int)(ModifierKeys.Control | ModifierKeys.Alt),
                    KeyCode = (int)Keys.R
                },
                new HotkeyItem
                {
                    Id = "show_ip",
                    Name = "显示IP",
                    Description = "显示本机IP地址和网关",
                    IsEnabled = true,
                    Modifier = (int)(ModifierKeys.Control | ModifierKeys.Alt),
                    KeyCode = (int)Keys.P
                },
                new HotkeyItem
                {
                    Id = "turn_off_monitor",
                    Name = "关闭显示器",
                    Description = "关闭显示器（按ESC取消）",
                    IsEnabled = true,
                    Modifier = (int)ModifierKeys.Control,
                    KeyCode = (int)Keys.F12
                }
            };
        }

        /// <summary>
        /// 加载热键配置
        /// </summary>
        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    _hotkeys = JsonSerializer.Deserialize<List<HotkeyItem>>(json) ?? GetDefaultHotkeys();
                }
                else
                {
                    _hotkeys = GetDefaultHotkeys();
                }
            }
            catch
            {
                _hotkeys = GetDefaultHotkeys();
            }
        }

        /// <summary>
        /// 保存热键配置
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                var directory = Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_hotkeys, options);
                File.WriteAllText(_settingsPath, json);
            }
            catch
            {
                // 忽略保存错误
            }
        }

        /// <summary>
        /// 注册所有热键
        /// </summary>
        /// <returns>冲突的热键列表</returns>
        public List<HotkeyItem> RegisterAll()
        {
            var conflicted = new List<HotkeyItem>();

            foreach (var hotkey in _hotkeys)
            {
                if (hotkey.IsEnabled)
                {
                    if (!TryRegister(hotkey))
                    {
                        hotkey.IsConflicted = true;
                        conflicted.Add(hotkey);
                    }
                    else
                    {
                        hotkey.IsConflicted = false;
                    }
                }
            }

            if (conflicted.Count > 0)
            {
                HotkeysConflicted?.Invoke(this, conflicted);
            }

            return conflicted;
        }

        /// <summary>
        /// 尝试注册单个热键
        /// </summary>
        private bool TryRegister(HotkeyItem hotkey)
        {
            try
            {
                // 先注销之前的
                if (_registeredIds.ContainsKey(hotkey.Id))
                {
                    Unregister(hotkey.Id);
                }

                var id = _hook.RegisterHotKey(
                    (ModifierKeys)hotkey.Modifier,
                    (Keys)hotkey.KeyCode
                );

                _registeredIds[hotkey.Id] = id;
                _idToHotkey[id] = hotkey;
                hotkey.IsConflicted = false;
                return true;
            }
            catch
            {
                hotkey.IsConflicted = true;
                return false;
            }
        }

        /// <summary>
        /// 注销指定热键
        /// </summary>
        public void Unregister(string hotkeyId)
        {
            if (_registeredIds.TryGetValue(hotkeyId, out int id))
            {
                _hook.UnregisterHotKey(id);
                _registeredIds.Remove(hotkeyId);
                _idToHotkey.Remove(id);
            }
        }

        /// <summary>
        /// 注销所有热键
        /// </summary>
        public void UnregisterAll()
        {
            foreach (var id in _registeredIds.Values)
            {
                _hook.UnregisterHotKey(id);
            }
            _registeredIds.Clear();
            _idToHotkey.Clear();
        }

        /// <summary>
        /// 更新单个热键配置
        /// </summary>
        public bool UpdateHotkey(string hotkeyId, ModifierKeys modifier, Keys key)
        {
            var hotkey = FindHotkey(hotkeyId);
            if (hotkey == null) return false;

            // 先注销旧的
            Unregister(hotkeyId);

            // 更新配置
            hotkey.Modifier = (int)modifier;
            hotkey.KeyCode = (int)key;

            if (hotkey.IsEnabled)
            {
                // 尝试注册新的
                if (!TryRegister(hotkey))
                {
                    return false; // 注册失败（冲突）
                }
            }

            SaveSettings();
            SettingsChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// 检查热键是否冲突（不实际注册）
        /// </summary>
        public bool CheckHotkeyConflict(string excludeHotkeyId, ModifierKeys modifier, Keys key)
        {
            // 检查是否与其他已注册的热键冲突
            foreach (var hotkey in _hotkeys)
            {
                if (hotkey.Id == excludeHotkeyId || !hotkey.IsEnabled) continue;

                if (hotkey.Modifier == (int)modifier && hotkey.KeyCode == (int)key)
                {
                    return true; // 冲突
                }
            }
            return false;
        }

        /// <summary>
        /// 启用/禁用热键
        /// </summary>
        public void SetEnabled(string hotkeyId, bool enabled)
        {
            var hotkey = FindHotkey(hotkeyId);
            if (hotkey == null) return;

            if (enabled)
            {
                TryRegister(hotkey);
            }
            else
            {
                Unregister(hotkeyId);
            }

            hotkey.IsEnabled = enabled;
            SaveSettings();
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 重置所有热键为默认
        /// </summary>
        public void ResetToDefault()
        {
            UnregisterAll();
            _hotkeys = GetDefaultHotkeys();
            SaveSettings();
            RegisterAll();
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 查找热键配置
        /// </summary>
        public HotkeyItem? FindHotkey(string id)
        {
            foreach (var h in _hotkeys)
            {
                if (h.Id == id) return h;
            }
            return null;
        }

        /// <summary>
        /// 根据按下的键查找热键
        /// </summary>
        public HotkeyItem? FindHotkeyByKey(ModifierKeys modifier, Keys key)
        {
            foreach (var h in _hotkeys)
            {
                if (h.Modifier == (int)modifier && h.KeyCode == (int)key && h.IsEnabled)
                {
                    return h;
                }
            }
            return null;
        }

        #region IDisposable

        public void Dispose()
        {
            UnregisterAll();
            _hook.Dispose();
        }

        #endregion
    }
}
