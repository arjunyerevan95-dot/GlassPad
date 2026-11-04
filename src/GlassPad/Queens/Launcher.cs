// SPDX-License-Identifier: GPL-3.0-or-later
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GlassPad.Engine;
using Microsoft.Win32;
namespace GlassPad.Queens;
internal sealed class Launcher : Window
{
    private GameProfile _profile=Settings.Load();
    private readonly ContactRouter _previewRouter;
    private readonly ControlSurface _preview;
    private GameplayOverlay? _overlay;
    private readonly TextBlock _status;
    public Launcher(string[] args)
    {
        Title="GlassPad • Queen’s Domain • Editable pilot";Width=1160;Height=820;MinWidth=820;MinHeight=600;Background=new SolidColorBrush(Color.FromRgb(17,24,22));Foreground=Brushes.WhiteSmoke;
        _previewRouter=new(_profile);_preview=new(_previewRouter){Preview=true};
        var root=new DockPanel{Margin=new Thickness(22)};Content=root;
        var title=new TextBlock{Text="GlassPad / Queen’s Domain",FontSize=26,Margin=new Thickness(0,0,0,6)};DockPanel.SetDock(title,Dock.Top);root.Children.Add(title);
        var sub=new TextBlock{Text=$"{_profile.Approval.ToUpperInvariant()} LAYOUT — editable positions, size and opacity. Your original GlassPad settings are separate.",Foreground=Brushes.LightGray,Margin=new Thickness(0,0,0,16)};DockPanel.SetDock(sub,Dock.Top);root.Children.Add(sub);
        var bar=new WrapPanel{Margin=new Thickness(0,0,0,14)};DockPanel.SetDock(bar,Dock.Top);root.Children.Add(bar);
        void Button(string label,Action action){var b=new Button{Content=label,Padding=new Thickness(14,9,14,9),Margin=new Thickness(0,0,8,8)};b.Click+=(_,_)=>action();bar.Children.Add(b);}
        Button("Start overlay",StartOverlay);Button("Customize Controls",Edit);Button("Radial preview",()=>{_previewRouter.PreviewPanel(_previewRouter.View(Native.Now).Panel=="radial"?"":"radial");_preview.InvalidateVisual();});
        Button("Touch targets",()=>{_preview.DebugTargets=!_preview.DebugTargets;_preview.InvalidateVisual();});
        Button("Choose profile",ChooseProfile);Button("Export profile",Export);Button("Licenses",()=>new LicenseWindow{Owner=this}.ShowDialog());
        _status=new TextBlock{Text="Floating left movement · open swipe look · hold either attack and drag to look\nActions → More → Customize Controls · Ctrl+Alt+G hide/recover · Ctrl+Alt+M menu mode · Ctrl+Alt+Q exit overlay",Foreground=Brushes.LightGray,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,0)};
        DockPanel.SetDock(_status,Dock.Bottom);root.Children.Add(_status);
        var view=new Viewbox{Stretch=Stretch.Uniform,Child=_preview};_preview.Width=1280;_preview.Height=800;_previewRouter.SetSize(1280,800);root.Children.Add(view);
        Closed+=(_,_)=>{_overlay?.Close();Application.Current.Shutdown();};
        Loaded+=async(_,_)=>{
            var index=Array.IndexOf(args,"--render-previews");
            if(index>=0&&index+1<args.Length){RenderPreviews(args[index+1]);Close();}
            else if((index=Array.IndexOf(args,"--ui-checks"))>=0&&index+1<args.Length){UiChecks.Run(this,args[index+1]);Close();}
            else if((index=Array.IndexOf(args,"--input-checks"))>=0&&index+1<args.Length){await TouchIntegration.RunAsync(this,args[index+1]);Close();}
            else if(args.Contains("--live"))StartOverlay();
            else if(args.Contains("--editor"))Edit();
        };
    }
    private void StartOverlay()
    {
        if(_overlay is not null){_status.Text="Overlay is running. Switch to Queen’s Domain. Ctrl+Alt+G recovers hidden controls.";return;}
        _overlay=new(_profile);_overlay.CustomizeRequested+=Edit;_overlay.ProfilesRequested+=ChooseProfile;
        _overlay.AutoSelected+=p=>{_profile=p;_previewRouter.Configure(p);_preview.InvalidateVisual();};
        _overlay.Closed+=(_,_)=>{_overlay=null;_status.Text="Overlay closed • all virtual input released.";};_overlay.Show();
        _status.Text="Overlay running. Return to Queen’s Domain; input activates only for the verified game executable in foreground.\nCtrl+Alt+G hide/recover · Ctrl+Alt+M manual menu mode · Ctrl+Alt+Q stop overlay";
        WindowState=WindowState.Minimized;
    }
    private void Edit()
    {
        _overlay?.Router.SetEnabled(false);
        var editor=new ProfileEditor(_profile){Owner=this};
        if(editor.ShowDialog()==true){_profile=editor.Result!;_previewRouter.Configure(_profile);_preview.InvalidateVisual();_status.Text="Saved your editable profile (position, size, opacity and assignments).";}
        _overlay?.Resume(_profile);
    }
    private void ChooseProfile()
    {
        _overlay?.Router.SetEnabled(false);
        var dialog=new OpenFileDialog{Filter="GlassPad profile (*.json)|*.json",Title="Select a game profile"};
        if(dialog.ShowDialog(this)==true)
        {try{var p=ProfileStore.Parse(File.ReadAllText(dialog.FileName));p.Approval="Draft";_profile=p;_previewRouter.Configure(p);_preview.InvalidateVisual();_status.Text="Selected "+p.Title+" • Draft for this session";}catch(Exception ex){MessageBox.Show(this,ex.Message,"Profile rejected");}}
        _overlay?.Resume(_profile);
    }
    private void Export()
    {var d=new SaveFileDialog{Filter="JSON profile|*.json",FileName="queens-domain-draft.json"};if(d.ShowDialog(this)==true)ProfileStore.SaveAtomic(d.FileName,_profile);}
    private void RenderPreviews(string directory)
    {
        Directory.CreateDirectory(directory);
        void Render(string name,string panel,bool debug)
        {
            _previewRouter.PreviewPanel(panel);_preview.DebugTargets=debug;_preview.Measure(new Size(1280,800));_preview.Arrange(new Rect(0,0,1280,800));_preview.InvalidateVisual();_preview.UpdateLayout();
            var bitmap=new RenderTargetBitmap(1920,1200,144,144,PixelFormats.Pbgra32);bitmap.Render(_preview);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(directory,name+".png"));encoder.Save(file);
        }
        Render("queens-domain-draft-overlay","",false);Render("queens-domain-draft-radial","radial",false);Render("queens-domain-draft-targets","",true);
        ProfileStore.SaveAtomic(Path.Combine(directory,"queens-domain-draft.json"),_profile);
    }
}
