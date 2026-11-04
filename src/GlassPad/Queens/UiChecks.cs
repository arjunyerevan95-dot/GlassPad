// SPDX-License-Identifier: GPL-3.0-or-later
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GlassPad.Engine;
namespace GlassPad.Queens;
internal static class UiChecks
{
    internal static void Run(Window owner,string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);var log=new List<string>();var temp=Path.Combine(Path.GetTempPath(),"glasspad-ui-"+Guid.NewGuid());Settings.TestRoot=temp;
        try
        {
            var profile=Settings.Load();ProfileStore.SaveAtomic(Settings.ProfilePath,profile);var original=File.ReadAllText(Settings.ProfilePath);
            var editor=new ProfileEditor(profile){Owner=owner};
            editor.Loaded+=(_,_)=>editor.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>{
                try
                {
                    var d=editor.TestDiagnostics();if(d.Fields<100||d.Extent<=d.Viewport)throw new Exception("Editor inspector is not scrollable: "+d);
                    log.Add($"PASS actual editor has {d.Fields} fields; scroll extent {d.Extent:F0} > viewport {d.Viewport:F0}");
                    editor.TestChange("X",.72);editor.TestChange("Size / short edge",.12);editor.TestChange("Control opacity",.44);
                    var bitmap=new RenderTargetBitmap((int)editor.ActualWidth,(int)editor.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(editor);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var f=File.Create(Path.Combine(outputDirectory,"queens-domain-editor.png")))encoder.Save(f);
                }
                catch(Exception ex){log.Add("FAIL "+ex);}
                finally{editor.TestCancel();}
            }));
            editor.ShowDialog();if(File.ReadAllText(Settings.ProfilePath)!=original||ProfileStore.Serialize(profile)!=original)throw new Exception("Editor Cancel changed profile");log.Add("PASS actual editor cancellation preserves original and disk");
            var saveEditor=new ProfileEditor(profile){Owner=owner};saveEditor.Loaded+=(_,_)=>saveEditor.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>{
                try{saveEditor.TestChange("X",.72);saveEditor.TestChange("Size / short edge",.12);saveEditor.TestChange("Control opacity",.44);saveEditor.TestChange("Global opacity",.63);saveEditor.TestSave();}
                catch(Exception ex){log.Add("FAIL "+ex);saveEditor.TestCancel();}
            }));
            saveEditor.ShowDialog();var saved=ProfileStore.Parse(File.ReadAllText(Settings.ProfilePath));var first=saved.Controls.First(c=>c.Id=="primary");
            if(Math.Abs(first.X-.72)>.001||Math.Abs(first.Size-.12)>.001||Math.Abs(first.Opacity-.44)>.001||Math.Abs(saved.Opacity-.63)>.001)throw new Exception("Editor Save failed to persist actual bound values");
            if(profile.Controls[0].X==.72)throw new Exception("Editor mutated supplied profile");log.Add("PASS actual editor Save persists position, size, control opacity and global opacity atomically");
        }
        catch(Exception ex){log.Add("FAIL "+ex);}
        finally
        {Settings.TestRoot=null;try{Directory.Delete(temp,true);}catch{}File.WriteAllLines(Path.Combine(outputDirectory,"ui-checks.log"),log);}
    }
}
