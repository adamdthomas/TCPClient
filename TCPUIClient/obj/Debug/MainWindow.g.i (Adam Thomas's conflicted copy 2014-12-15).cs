﻿#pragma checksum "..\..\MainWindow.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "42DA6A2243EE8800090B2E8363ACCF5D"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18444
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace TCPUIClient {
    
    
    /// <summary>
    /// MainWindow
    /// </summary>
    public partial class MainWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 18 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox txMessage;
        
        #line default
        #line hidden
        
        
        #line 19 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btnConnect;
        
        #line default
        #line hidden
        
        
        #line 20 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btnDisconnect;
        
        #line default
        #line hidden
        
        
        #line 21 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox txServername;
        
        #line default
        #line hidden
        
        
        #line 23 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox txPort;
        
        #line default
        #line hidden
        
        
        #line 25 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btnSend;
        
        #line default
        #line hidden
        
        
        #line 26 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox txMain;
        
        #line default
        #line hidden
        
        
        #line 29 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox txStatus;
        
        #line default
        #line hidden
        
        
        #line 30 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox cbGameEnabled;
        
        #line default
        #line hidden
        
        
        #line 31 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox cbOutputGPData;
        
        #line default
        #line hidden
        
        
        #line 34 "..\..\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Slider slDeadZone;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/TCPUIClient;component/mainwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\MainWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 5 "..\..\MainWindow.xaml"
            ((System.Windows.Controls.Grid)(target)).Initialized += new System.EventHandler(this.Grid_Initialized);
            
            #line default
            #line hidden
            return;
            case 2:
            this.txMessage = ((System.Windows.Controls.TextBox)(target));
            
            #line 18 "..\..\MainWindow.xaml"
            this.txMessage.TextChanged += new System.Windows.Controls.TextChangedEventHandler(this.txMessage_TextChanged);
            
            #line default
            #line hidden
            
            #line 18 "..\..\MainWindow.xaml"
            this.txMessage.KeyDown += new System.Windows.Input.KeyEventHandler(this.txMessage_KeyDown);
            
            #line default
            #line hidden
            return;
            case 3:
            this.btnConnect = ((System.Windows.Controls.Button)(target));
            
            #line 19 "..\..\MainWindow.xaml"
            this.btnConnect.Click += new System.Windows.RoutedEventHandler(this.btnConnect_Click);
            
            #line default
            #line hidden
            return;
            case 4:
            this.btnDisconnect = ((System.Windows.Controls.Button)(target));
            
            #line 20 "..\..\MainWindow.xaml"
            this.btnDisconnect.Click += new System.Windows.RoutedEventHandler(this.btnDisconnect_Click);
            
            #line default
            #line hidden
            return;
            case 5:
            this.txServername = ((System.Windows.Controls.TextBox)(target));
            
            #line 21 "..\..\MainWindow.xaml"
            this.txServername.LostFocus += new System.Windows.RoutedEventHandler(this.txServername_LostFocus);
            
            #line default
            #line hidden
            
            #line 21 "..\..\MainWindow.xaml"
            this.txServername.KeyDown += new System.Windows.Input.KeyEventHandler(this.txServername_KeyDown);
            
            #line default
            #line hidden
            return;
            case 6:
            this.txPort = ((System.Windows.Controls.TextBox)(target));
            
            #line 23 "..\..\MainWindow.xaml"
            this.txPort.TextChanged += new System.Windows.Controls.TextChangedEventHandler(this.txPort_TextChanged);
            
            #line default
            #line hidden
            return;
            case 7:
            this.btnSend = ((System.Windows.Controls.Button)(target));
            
            #line 25 "..\..\MainWindow.xaml"
            this.btnSend.Click += new System.Windows.RoutedEventHandler(this.btnSend_Click);
            
            #line default
            #line hidden
            return;
            case 8:
            this.txMain = ((System.Windows.Controls.TextBox)(target));
            return;
            case 9:
            this.txStatus = ((System.Windows.Controls.TextBox)(target));
            return;
            case 10:
            this.cbGameEnabled = ((System.Windows.Controls.CheckBox)(target));
            
            #line 30 "..\..\MainWindow.xaml"
            this.cbGameEnabled.Click += new System.Windows.RoutedEventHandler(this.cbGameEnabled_Click);
            
            #line default
            #line hidden
            return;
            case 11:
            this.cbOutputGPData = ((System.Windows.Controls.CheckBox)(target));
            
            #line 31 "..\..\MainWindow.xaml"
            this.cbOutputGPData.Click += new System.Windows.RoutedEventHandler(this.cbOutputGPData_Click);
            
            #line default
            #line hidden
            
            #line 31 "..\..\MainWindow.xaml"
            this.cbOutputGPData.Checked += new System.Windows.RoutedEventHandler(this.cbOutputGPData_Checked);
            
            #line default
            #line hidden
            return;
            case 12:
            
            #line 33 "..\..\MainWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.Button_Click);
            
            #line default
            #line hidden
            return;
            case 13:
            this.slDeadZone = ((System.Windows.Controls.Slider)(target));
            
            #line 34 "..\..\MainWindow.xaml"
            this.slDeadZone.ValueChanged += new System.Windows.RoutedPropertyChangedEventHandler<double>(this.Slider_ValueChanged);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

