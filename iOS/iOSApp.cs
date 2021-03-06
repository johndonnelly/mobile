﻿using System;
using System.Collections.Generic;
using CoreGraphics;
using SplitAndMerge;
using UIKit;

namespace scripting.iOS
{
    public class iOSTab
    {
        UIViewController m_tab;
        List<iOSVariable> m_views = new List<iOSVariable>();
        UIImage m_image;

        public iOSTab(UIViewController tab, string text)
        {
            m_tab = tab;
            OriginalText = text;
            Text = text;
        }
        public string OriginalText;
        public string Text
        {
            get { return m_tab.Title; }
            set { m_tab.Title = value; }
        }
        public UIImage Image
        {
            get { return m_tab.TabBarItem.Image; }
            set
            {
                m_image = value;
                if (m_image != null)
                {
                    m_image = m_image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal);
                }
                m_tab.TabBarItem.Image = m_image;
            }
        }
        public void AddView(iOSVariable view)
        {
            m_views.Add(view);
        }
        public void RemoveView(iOSVariable view)
        {
            if (view == null)
            {
                return;
            }
            view.ViewY?.RemoveFromSuperview();
            view.ViewX?.RemoveFromSuperview();
            m_views.Remove(view);
        }
        public void ShowTab(bool showIt = true)
        {
            for (int i = 0; i < m_views.Count; i++)
            {
                iOSApp.ShowView(m_views[i], showIt, true);
            }
        }
        public void RemoveAll()
        {
            while (m_views.Count > 0)
            {
                RemoveView(m_views[0]);
            }
        }

        public UIColor BackgroundColor
        {
            set
            {
                m_tab.View.BackgroundColor = value;
            }
        }
        //public UIColor BackgroundImage {
        //    set {    //m_tab.bac =value;    }
        //}
    }

    public class iOSApp : UITabBarController
    {
        static List<iOSTab> m_tabs = new List<iOSTab>();
        static iOSTab m_activeTab;
        static List<iOSVariable> m_hiddenViews = new List<iOSVariable>();
        static List<iOSVariable> m_nonTabViews = new List<iOSVariable>();
        static Dictionary<iOSVariable, iOSTab> m_view2Tab =
           new Dictionary<iOSVariable, iOSTab>();

        public static Action<int> TabSelectedDelegate;

        static UIInterfaceOrientationMask m_orientationMask = UIInterfaceOrientationMask.AllButUpsideDown;

        int m_selectedTab = -1;

        public static iOSApp Instance { set; get; }
        public int SelectedTab { get { return m_selectedTab; } }

        static Dictionary<string, int> m_allTabs = new Dictionary<string, int>();

        public iOSApp()
        {
            Instance = this;

        }

        bool m_isHidden;

        public void HideTabBar()
        {
            if (m_isHidden)
                return;

            var screenRect = UIScreen.MainScreen.Bounds;
            var height = screenRect.Height;

            if (UIApplication.SharedApplication.StatusBarOrientation == UIInterfaceOrientation.LandscapeLeft
                || UIApplication.SharedApplication.StatusBarOrientation == UIInterfaceOrientation.LandscapeRight)
            {
                height = screenRect.Width;
            }

            UIView.BeginAnimations(null);
            UIView.SetAnimationDuration(0.4);

            foreach (UIView view in this.View.Subviews)
            {
                if (view is UITabBar)
                    view.Frame = new CGRect(view.Frame.X, height, view.Frame.Width, view.Frame.Height);
                else
                {
                    view.Frame = new CGRect(view.Frame.X, view.Frame.Y, view.Frame.Width, height);
                    view.BackgroundColor = UIColor.Black;
                }
            }

            UIView.CommitAnimations();

            m_isHidden = true;
        }
        public static string Orientation
        {
            get
            {
                return IsLandscape ? "Landscape" : "Portrait";
            }
        }
        public static bool IsLandscape
        {
            get
            {
                return UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.LandscapeLeft ||
                       UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.LandscapeRight;
            }
        }
        public static bool ValidOrientation
        {
            get
            {
                return UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.LandscapeLeft ||
                       UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.LandscapeRight ||
                       UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.Portrait;
            }
        }

        public static UIInterfaceOrientationMask OrientationMask
        {
            set { m_orientationMask = value; }
        }
        public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations()
        {
            return m_orientationMask;
        }

        public static int CurrentOffset { get; set; }

        public void OffsetTabBar(bool move = true)
        {
            bool down = ViewControllers == null || ViewControllers.Length == 0;
            var tabFrame = TabBar.Frame;
            if (move)
            {
                int offset = down ? (int)tabFrame.Size.Height : -1 * (int)tabFrame.Size.Height;
                //        offset = 150;
                tabFrame.Y += offset; //.Offset(0, offset);
                TabBar.Frame = tabFrame;
            }
            CurrentOffset = down ? 0 : (int)tabFrame.Size.Height;
        }

        static int GetMinBottomOffset()
        {
            if (Instance.m_selectedTab < 0)
            {
                return UtilsiOS.IsiPhoneX() ? UtilsiOS.ROOT_BOTTOM_MIN_X : 0;
            }
            return UtilsiOS.ROOT_BOTTOM_MIN;
        }

        public static double AdjustSize(double size)
        {
            double newSize = size;

            if (UtilsiOS.IsiPhoneX())
            {
                newSize = size / 1.5f;
            }
            else if (UtilsiOS.IsiPhonePlus())
            {
                newSize = size / 1.3f;
            }
            return newSize;
        }

        public static int GetVerticalOffset()
        {
            int offset = GetMinBottomOffset();
            if (iOSApp.CurrentOffset == 0)
            {
                return offset;
            }
            offset += (int)(iOSApp.CurrentOffset * 0.8);

            // Special dealing with iPhone X:
            if (UtilsiOS.IsiPhoneX())
            {
                offset += 6;
            }
            if (IsLandscape)
            {
                offset -= 2;
            }

            return offset;
        }

        public void OnTabSelected(object sender, UITabBarSelectionEventArgs e)
        {
            m_selectedTab = (int)e.ViewController.TabBarController.SelectedIndex;
            SelectTab(m_selectedTab);
            TabSelectedDelegate?.Invoke(m_selectedTab);
        }
        public static void SelectTab(int selectedTab)
        {
            Instance.m_selectedTab = selectedTab;
            Instance.SelectedIndex = Instance.m_selectedTab;

            m_activeTab = m_tabs[selectedTab];
            for (int i = 0; i < m_tabs.Count; i++)
            {
                m_tabs[i].ShowTab(i == selectedTab);
            }
        }

        public static void AddView(iOSVariable view)
        {
            if (m_activeTab == null)
            {
                m_nonTabViews.Add(view);
                return;
            }
            m_activeTab.AddView(view);
            m_view2Tab[view] = m_activeTab;
        }
        public static void RemoveView(iOSVariable view)
        {
            iOSTab tab;
            if (m_view2Tab.TryGetValue(view, out tab))
            {
                tab.RemoveView(view);
            }
            else
            {
                RemoveNonTabView(view);
            }
        }
        public static void RemoveAllNonTabViews()
        {
            while (m_nonTabViews.Count > 0)
            {
                RemoveNonTabView(m_nonTabViews[0]);
            }
        }
        public static void RemoveNonTabView(iOSVariable view)
        {
            view.ViewY?.RemoveFromSuperview();
            view.ViewX?.RemoveFromSuperview();
            m_nonTabViews.Remove(view);
        }
        public static void RemoveTabViews(int tabId)
        {
            if (m_tabs.Count <= tabId || tabId < 0)
            {
                throw new ArgumentException("Tab " + tabId + " doesn't exist.");
            }
            m_tabs[tabId].RemoveAll();
        }
        public static void RemoveAll()
        {
            RemoveAllNonTabViews();
            for (int i = 0; i < m_tabs.Count; i++)
            {
                m_tabs[i].RemoveAll();
            }
        }

        public void Run()
        {
            // If there is no tabbbar, move the tabs view down:
            OffsetTabBar();

            this.ViewControllerSelected += OnTabSelected;

            CustomInit.InitAndRunScript();

            if (m_selectedTab >= 0)
            {
                SelectTab(m_selectedTab);
            }
            else if (TabBar != null)
            {
                TabBar.Hidden = true;
            }
        }

        private void RunScript()
        {
            throw new NotImplementedException();
        }

        public static bool TabExists(string text)
        {
            return m_allTabs.ContainsKey(text);
        }
        public static bool SelectTab(string text)
        {
            int tabId = 0;
            if (m_allTabs.TryGetValue(text, out tabId))
            {
                SelectTab(tabId);
                return true;
            }
            return false;
        }
        public static void AddTab(string text, string selectedImageName, string notSelectedImageName = null)
        {
            var selImage = UtilsiOS.LoadImage(selectedImageName);
            if (selImage == null)
            {
                throw new ArgumentException("Image [" + selectedImageName + "] not found.");
            }
            selImage = selImage.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal);

            UIViewController tab = new UIViewController();
            tab.Title = text;
            tab.TabBarItem.SelectedImage = selImage;
            tab.TabBarItem.Image = selImage;

            if (notSelectedImageName != null)
            {
                var image = UtilsiOS.LoadImage(notSelectedImageName);
                if (image == null)
                {
                    throw new ArgumentException("Image [" + notSelectedImageName + "] not found.");
                }
                image = image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal);
                tab.TabBarItem.Image = image;
            }

            List<UIViewController> controllers = Instance.ViewControllers == null ?
                new List<UIViewController>() :
                new List<UIViewController>(Instance.ViewControllers);
            controllers.Add(tab);
            Instance.ViewControllers = controllers.ToArray();

            m_activeTab = new iOSTab(tab, text);
            m_tabs.Add(m_activeTab);
            if (controllers.Count == 1)
            {
                // Lift the tabbar back up:
                Instance.OffsetTabBar();
            }

            m_allTabs[text] = m_tabs.Count - 1;
            SelectTab(m_tabs.Count - 1);
        }

        public static void TranslateTabs()
        {
            foreach (var tab in m_tabs)
            {
                string translated = Localization.GetText(tab.OriginalText);
                tab.Text = translated;
            }
        }

        public static void ShowView(iOSVariable view, bool showIt, bool tabChange)
        {
            bool explicitlyHidden = m_hiddenViews.Contains(view);
            if (explicitlyHidden && tabChange)
            {
                return;
            }

            /*UIView uiview = view.ViewX;
            if (uiview == null) {
              uiview = AppDelegate.GetCurrentView();
            }
            uiview.Hidden = !showIt;*/
            view.ShowHide(showIt);

            if (tabChange)
            {
                return;
            }
            if (!showIt && !explicitlyHidden)
            {
                m_hiddenViews.Add(view);
            }
            else if (showIt && explicitlyHidden)
            {
                m_hiddenViews.Remove(view);
            }
        }

        public override void DidReceiveMemoryWarning()
        {
            // Releases the view if it doesn't have a superview.
            base.DidReceiveMemoryWarning();
        }

        public override void ViewDidLoad()
        {
            if (this.InterfaceOrientation == UIInterfaceOrientation.Portrait
                || this.InterfaceOrientation == UIInterfaceOrientation.PortraitUpsideDown)
            {
                // portrait
            }
            else
            {
                // landsacpe
            }
            base.ViewDidLoad();
        }
    }
}
