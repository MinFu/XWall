﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using XWall.Properties;

namespace XWall {
    partial class MainWindow {
        System.Windows.Forms.MenuItem profileContextMenu = new System.Windows.Forms.MenuItem(resources["Profiles"] as string);
        System.Windows.Forms.MenuItem proxyTypeContextMenu = new System.Windows.Forms.MenuItem(resources["ProxyTypeContextMenu"] as string);
        System.Windows.Forms.MenuItem proxyTypeGaMenuItem = new System.Windows.Forms.MenuItem(resources["GaContextMenu"] as string);
        System.Windows.Forms.MenuItem proxyTypeSshMenuItem = new System.Windows.Forms.MenuItem(resources["SshTunnelContextMenu"] as string);
        System.Windows.Forms.MenuItem proxyTypeHttpMenuItem = new System.Windows.Forms.MenuItem(resources["HttpProxyContextMenu"] as string);
        System.Windows.Forms.MenuItem proxyTypeSocksMenuItem = new System.Windows.Forms.MenuItem(resources["Socks5ProxyContextMenu"] as string);

        void InitializeBinding() {
            //BASIC SETTINGS
            //proxy type

            proxyTypeGaMenuItem.RadioCheck = true;
            proxyTypeSshMenuItem.RadioCheck = true;
            proxyTypeHttpMenuItem.RadioCheck = true;
            proxyTypeSocksMenuItem.RadioCheck = true;

            proxyTypeContextMenu.MenuItems.Add(proxyTypeGaMenuItem);
            proxyTypeContextMenu.MenuItems.Add(proxyTypeSshMenuItem);
            proxyTypeContextMenu.MenuItems.Add(proxyTypeHttpMenuItem);
            proxyTypeContextMenu.MenuItems.Add(proxyTypeSocksMenuItem);

            initProxyType();

            settings.PropertyChanged += (o, a) => {
                switch (a.PropertyName) {
                    case "ProxyType": break;
                    default: return;
                }

                initProxyType();
            };

            var gaSelected = new Action(() => {
                if (settings.ProxyType != "GA") {
                    if (Directory.Exists(App.AppDataDirectory + settings.GaFolderName)) {
                        settings.ProxyType = "GA";
                    }
                    else {
                        initProxyType();
                        var r = MessageBox.Show(resources["GoAgentNotInstalledMessage"] as string, resources["XWall"] as string, MessageBoxButton.OKCancel);
                        if (r == MessageBoxResult.OK) {
                            settings.ToUseGoAgent = true;
                            downloadUpdate(true);
                            aboutTabItem.Focus();
                        }
                    }
                }
            });

            proxyTypeGaRadio.Checked += (sender, e) => {
                gaSelected();
            };

            proxyTypeGaMenuItem.Click += (sender, e) => {
                gaSelected();
            };

            proxyTypeSshRadio.Checked += (sender, e) => {
                if (settings.ProxyType != "SSH") {
                    settings.ProxyType = "SSH";
                }
            };

            proxyTypeSshMenuItem.Click += (sender, e) => {
                if (settings.ProxyType != "SSH") {
                    settings.ProxyType = "SSH";
                }
            };

            proxyTypeHttpRadio.Checked += (sender, e) => {
                if (settings.ProxyType != "HTTP") {
                    settings.ProxyType = "HTTP";
                }
            };

            proxyTypeHttpMenuItem.Click += (sender, e) => {
                if (settings.ProxyType != "HTTP") {
                    settings.ProxyType = "HTTP";
                }
            };

            proxyTypeSocksRadio.Checked += (sender, e) => {
                if (settings.ProxyType != "SOCKS5") {
                    settings.ProxyType = "SOCKS5";
                }
            };
            proxyTypeSocksMenuItem.Click += (sender, e) => {
                if (settings.ProxyType != "SOCKS5") {
                    settings.ProxyType = "SOCKS5";
                }
            };

            //goagent information
            gaProfilesList.SelectionChanged += (sender, e) => {
                var value = gaProfilesList.SelectedValue as string;
                if (settings.GaProfile != value) {
                    settings.GaProfile = value;
                }
            };

            gaProfilesList.Text = settings.GaProfile;

            //ssh information
            UIBinding.bindTextBox(sshServerTextBox, "SshServer");
            UIBinding.bindTextBox(sshPortTextBox, "SshPort");
            UIBinding.bindTextBox(sshUsernameTextBox, "SshUsername");
            UIBinding.bindTextBox(sshPasswordBox, "SshPassword");

            //http information
            UIBinding.bindTextBox(httpServerTextBox, "HttpServer");
            UIBinding.bindTextBox(httpPortTextBox, "HttpPort");

            //socks information
            UIBinding.bindTextBox(socksServerTextBox, "SocksServer");
            UIBinding.bindTextBox(socksPortTextBox, "SocksPort");

            //ADVANCED SETTINGS
            //x-wall
            UIBinding.bindCheckBox(autoStartCheckBox, "AutoStart");
            UIBinding.bindCheckBox(autoSetProxyCheckBox, "SetProxyAutomatically");

            settings.PropertyChanged += (sender, e) => {
                if (e.PropertyName == "SetProxyAutomatically") {
                    if (settings.SetProxyAutomatically) {
                        Operation.Proxies.SetXWallProxy();
                    }
                    else {
                        Operation.Proxies.RestoreProxy();
                    }
                }
            };

            UIBinding.bindCheckBox(listenToLocalOnlyCheckBox, "ListenToLocalOnly");
            UIBinding.bindTextBox(proxyPortTextBox, "ProxyPort");

            UIBinding.bindCheckBox(useIntranetProxyCheckBox, "UseIntranetProxy");
            UIBinding.bindTextBox(intranetProxyServerTextBox, "IntranetProxyServer");
            UIBinding.bindTextBox(intranetProxyPortTextBox, "IntranetProxyPort");
            
            //goagent
            UIBinding.bindTextBox(gaAppIdsTextBox, "GaAppIds");
            UIBinding.bindTextBox(gaPortTextBox, "GaPort");

            //ssh
            UIBinding.bindCheckBox(sshNotificationCheckBox, "SshNotification");
            UIBinding.bindCheckBox(sshCompressionCheckBox, "SshCompression");
            UIBinding.bindCheckBox(sshAutoReconnectCheckBox, "SshAutoReconnect");
            UIBinding.bindCheckBox(sshReconnectAnyConditionCheckBox, "SshReconnectAnyCondition");
            UIBinding.bindCheckBox(sshUsePlonkCheckBox, "SshUsePlonk");
            UIBinding.bindTextBox(sshPlonkKeywordTextBox, "SshPlonkKeyword");
            UIBinding.bindTextBox(sshSocksPortTextBox, "SshSocksPort");

            //PROFILES
            //ssh profiles
            sshProfiles = new Profile.SshProfilesCollection("SshProfiles");
            sshProfilesListBox.ItemsSource = sshProfiles.Items;
            sshProfilesList.ItemsSource = sshProfiles.Items;

            sshProfileEditGrid.IsEnabled =
            sshRemoveProfileButton.IsEnabled =
                sshProfilesListBox.SelectedItem != null;

            sshProfilesListBox.SelectionChanged += (sender, e) => {
                sshProfileEditGrid.IsEnabled =
                sshRemoveProfileButton.IsEnabled =
                    sshProfilesListBox.SelectedItem != null;
            };

            updateSshProfileEdit();
            updateSshInformation();


            Profile.DefaultProfile selectedSshProfile = null;

            if (settings.SshSelectedProfileIndex >= 0 && settings.SshSelectedProfileIndex < sshProfiles.Items.Count) {
                selectedSshProfile = sshProfiles.Items[settings.SshSelectedProfileIndex];
            }

            sshProfilesListBox.SelectionChanged += (sender, e) => {
                updateSshProfileEdit();
            };

            sshProfilesList.SelectedIndex = settings.SshSelectedProfileIndex;

            sshProfilesList.SelectionChanged += (sender, e) => {
                var index = sshProfilesList.SelectedIndex;
                if (settings.SshSelectedProfileIndex != index) {
                    settings.SshSelectedProfileIndex = index;
                }
            };

            sshProfiles.Items.ListChanged += (sender, e) => {
                var index = sshProfilesList.SelectedIndex;
                if (settings.SshSelectedProfileIndex != index) {
                    settings.SshSelectedProfileIndex = index;
                }
            };

            bindProfileContextMenu();

            settings.PropertyChanged += (sender, e) => {
                if (e.PropertyName == "SshSelectedProfileIndex") {
                    var index = settings.SshSelectedProfileIndex;

                    if (index >= 0 && index < sshProfiles.Items.Count) {
                        if (index != sshProfilesList.SelectedIndex) {
                            sshProfilesList.SelectedIndex = index;
                        }

                        var profile = sshProfiles.Items[index];

                        if (profile != selectedSshProfile) {
                            selectedSshProfile = profile;

                            profileContextMenu.MenuItems[index].PerformClick();
                            updateSshInformation();

                            if (plink.IsConnected || plink.IsConnecting || plink.IsReconnecting) {
                                plink.Stop();
                                plink.Start();
                            }
                        }
                    }
                }
            };

            sshProfileName.LostFocus += (sender, e) => {
                var item = sshProfilesListBox.SelectedItem as Profile.SshProfile;
                var name = sshProfileName.Text.Trim().Replace("\t", " ");
                if (item.Name != name) {
                    if (name != "") {
                        item.Name = name;
                    }
                    sshProfileName.Text = item.Name;
                    updateSshInformation();
                    sshProfiles.Items.ResetItem(sshProfilesListBox.SelectedIndex);
                    var index = sshProfilesList.SelectedIndex;
                    if (sshProfilesList.SelectedItem == item) {
                        sshProfilesList.Text = item.Name;
                    }
                }
            };
            sshProfileServer.LostFocus += (sender, e) => {
                var item = sshProfilesListBox.SelectedItem as Profile.SshProfile;
                var server = sshProfileServer.Text.Trim().ToLower();
                if (item.Server != server) {
                    if (new Regex(@"^([a-z0-9-]+(?:\.[a-z0-9-]+)*|)$").IsMatch(server)) {
                        item.Server = server;
                    }
                    sshProfileServer.Text = item.Server;
                    updateSshInformation();
                    sshProfiles.Items.ResetItem(sshProfilesListBox.SelectedIndex);
                }
            };
            sshProfilePort.LostFocus += (sender, e) => {
                var item = sshProfilesListBox.SelectedItem as Profile.SshProfile;
                var port = sshProfilePort.Text.Trim();
                if (item.Port.ToString() != port) {
                    if (new Regex(@"^[1-9]\d*$").IsMatch(port)) {
                        item.Port = int.Parse(port);
                    }
                    sshProfilePort.Text = item.Port.ToString();
                    updateSshInformation();
                    sshProfiles.Items.ResetItem(sshProfilesListBox.SelectedIndex);
                }
            };
            sshProfileUsername.LostFocus += (sender, e) => {
                var item = sshProfilesListBox.SelectedItem as Profile.SshProfile;
                var username = sshProfileUsername.Text.Trim().Replace("\t", " ");
                if (item.Username != username) {
                    item.Username = username;
                    sshProfileUsername.Text = item.Username;
                    updateSshInformation();
                    sshProfiles.Items.ResetItem(sshProfilesListBox.SelectedIndex);
                }
            };
            sshProfilePassword.LostFocus += (sender, e) => {
                var item = sshProfilesListBox.SelectedItem as Profile.SshProfile;
                var password = sshProfilePassword.Password.Replace("\t", " ");
                if (item.Password != password) {
                    item.Password = password;
                    sshProfilePassword.Password = item.Password;
                    updateSshInformation();
                    sshProfiles.Items.ResetItem(sshProfilesListBox.SelectedIndex);
                }
            };
            sshProfileName.GotFocus += selectAllWhenGetFocus;
            sshProfileServer.GotFocus += selectAllWhenGetFocus;
            sshProfilePort.GotFocus += selectAllWhenGetFocus;
            sshProfileUsername.GotFocus += selectAllWhenGetFocus;
            sshProfilePassword.GotFocus += selectAllWhenGetFocus;

            //RULES
            UIBinding.bindCheckBox(forwardAllCheckBox, "ForwardAll");
            UIBinding.bindCheckBox(useOnlineRulesCheckBox, "UseOnlineRules");
            //UIBinding.bindCheckBox(submitNewRuleCheckBox, "SubmitNewRule");
            //UIBinding.bindCheckBox(useCustomRulesCheckBox, "UseCustomRules");
        }

        void selectAllWhenGetFocus(Object sender, EventArgs e) {
            if (sender is TextBox) {
                (sender as TextBox).SelectAll();
            }
            else if (sender is PasswordBox) {
                (sender as PasswordBox).SelectAll();
            }
        }

        void bindProfileContextMenu() {
            var items = sshProfiles.Items;

            var build = new Action(() => {
                profileContextMenu.MenuItems.Clear();

                var selectedIndex = settings.SshSelectedProfileIndex;
                System.Windows.Forms.MenuItem selectedItem = null;

                profileContextMenu.Visible = settings.ProxyType == "SSH" && items.Count > 0;

                for (int i = 0; i < items.Count; i++) {
                    var item = items[i];

                    var menuItem = new System.Windows.Forms.MenuItem(item.Name, (sender, e) => {
                        var theMenuItem = sender as System.Windows.Forms.MenuItem;
                        if (theMenuItem != selectedItem) {
                            if (selectedItem != null) {
                                selectedItem.Checked = false;
                            }
                            selectedItem = theMenuItem;
                            theMenuItem.Checked = true;
                            if (settings.SshSelectedProfileIndex != theMenuItem.Index) {
                                settings.SshSelectedProfileIndex = theMenuItem.Index;
                            }
                        }
                    });

                    menuItem.RadioCheck = true;
                    profileContextMenu.MenuItems.Add(menuItem);

                    if (i == selectedIndex) {
                        menuItem.Checked = true;
                        selectedItem = menuItem;
                    }
                }
            });

            build();

            items.ListChanged += (sender, e) => {
                build();
            };

            settings.PropertyChanged += (o, a) => {
                switch (a.PropertyName) {
                    case "ProxyType": break;
                    default: return;
                }

                build();
            };
        }

        void updateSshProfileEdit() {
            var item = sshProfilesListBox.SelectedItem as Profile.SshProfile;
            if (item == null) {
                sshProfileName.Text = "";
                sshProfileServer.Text = "";
                sshProfilePort.Text = "";
                sshProfileUsername.Text = "";
                sshProfilePassword.Password = "";
            }
            else {
                sshProfileName.Text = item.Name;
                sshProfileServer.Text = item.Server;
                sshProfilePort.Text = item.Port.ToString();
                sshProfileUsername.Text = item.Username;
                sshProfilePassword.Password = item.Password;
            }
        }

        void updateSshInformation() { 
            var profile = sshProfilesList.SelectedItem as Profile.SshProfile;

            if (profile != null) {
                if (settings.SshServer != profile.Server) {
                    settings.SshServer = profile.Server;
                }
                if (settings.SshPort != profile.Port) {
                    settings.SshPort = profile.Port;
                }
                if (settings.SshUsername != profile.Username) {
                    settings.SshUsername = profile.Username;
                }
                if (settings.SshPassword != profile.Password) {
                    settings.SshPassword = profile.Password;
                }
            }
        }

        void initProxyType() {
            //settings to control
            proxyTypeGaMenuItem.Checked = false;
            proxyTypeSshMenuItem.Checked = false;
            proxyTypeHttpMenuItem.Checked = false;
            proxyTypeSocksMenuItem.Checked = false;

            switch (settings.ProxyType) {
                case "GA":
                    proxyTypeGaRadio.IsChecked = true;
                    proxyTypeGaMenuItem.Checked = true;
                    break;
                case "SSH":
                    proxyTypeSshRadio.IsChecked = true;
                    proxyTypeSshMenuItem.Checked = true;
                    break;
                case "HTTP":
                    proxyTypeHttpRadio.IsChecked = true;
                    proxyTypeHttpMenuItem.Checked = true;
                    break;
                case "SOCKS5":
                    proxyTypeSocksRadio.IsChecked = true;
                    proxyTypeSocksMenuItem.Checked = true;
                    break;
            }

        }
    }

    partial class CustomRulesEditor {
        void InitializeBinding() {
            UIBinding.bindCheckBox(addSubdomainsCheckBox, "CustomRulesAddSubdomains");
        }
    }

    class IntegerToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return (int)value == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    class StringToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return string.IsNullOrEmpty((string)value) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    class StringToVisibilityNotConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return !string.IsNullOrEmpty((string)value) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    class IntegerToBooleanNotConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return (int)value == 0 ? true : false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    class BooleanNotConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    static class UIBinding {
        static Settings settings = Settings.Default;

        public static void bindTextBox(TextBox textBox, string name) {
            var binding = new Binding(name);
            binding.Source = settings;
            textBox.SetBinding(TextBox.TextProperty, binding);
            textBox.GotFocus += (sender, e) => {
                textBox.SelectAll();
            };
        }

        public static void bindTextBox(PasswordBox passwordBox, string name) {
            passwordBox.Password = settings[name] as string;
            passwordBox.PasswordChanged += (sender, e) => {
                var password = passwordBox.Password;
                if (settings[name] as string != password) {
                    settings[name] = password;
                }
            };
            settings.PropertyChanged += (sender, e) => {
                if (e.PropertyName == name) {
                    var password = settings[name] as string;
                    if (passwordBox.Password != password) {
                        passwordBox.Password = password;
                    }
                }
            };
            passwordBox.GotFocus += (sender, e) => {
                passwordBox.SelectAll();
            };
        }

        public static void bindCheckBox(CheckBox checkBox, string name) {
            var binding = new Binding(name);
            binding.Source = settings;
            checkBox.SetBinding(CheckBox.IsCheckedProperty, binding);
        }

    }
}
