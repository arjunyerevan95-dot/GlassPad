// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using GlassPad.Engine;

namespace GlassPad.Queens;
internal sealed class ControlSurface : FrameworkElement
{
    public ContactRouter Router { get; }
    public bool Preview { get; set; }
    public bool DebugTargets { get; set; }
    public string? SelectedId { get; set; }
    public string DriverStatus { get; set; }="Preview • no controller connected";
    public string GyroStatus { get; set; }="Gyro off";
    public ControlSurface(ContactRouter router){Router=router;IsHitTestVisible=false;}
    private static readonly Brush White=Brushes.WhiteSmoke;
    private static readonly Brush Mint=new SolidColorBrush(Color.FromRgb(166,218,193));
    private static readonly Brush Muted=new SolidColorBrush(Color.FromRgb(156,167,163));
    private static readonly Brush Ink=new SolidColorBrush(Color.FromArgb(224,16,23,22));
    private static readonly Brush Fill=new SolidColorBrush(Color.FromArgb(25,220,234,228));
    private static readonly Typeface Font=new("Segoe UI");
    private void Text(DrawingContext dc,string text,double x,double y,double size,Brush brush,bool center=false)
    {var ft=new FormattedText(text,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,Font,size,brush,VisualTreeHelper.GetDpi(this).PixelsPerDip);dc.DrawText(ft,new Point(center?x-ft.Width/2:x,y));}
    protected override void OnRender(DrawingContext dc)
    {
        var w=ActualWidth;var h=ActualHeight;if(w<1||h<1)return;
        var u=Math.Min(w,h);var p=Router.Profile;var v=Router.View(Native.Now);
        // Alpha 1 ensures Windows routes touches across the whole client-area surface.
        dc.DrawRectangle(Preview?new SolidColorBrush(Color.FromRgb(20,29,26)):new SolidColorBrush(Color.FromArgb(1,0,0,0)),null,new Rect(0,0,w,h));
        if(Preview)
        {
            var line=new Pen(new SolidColorBrush(Color.FromArgb(12,190,214,201)),1);
            for(var x=u*.1;x<w;x+=u*.1)dc.DrawLine(line,new(x,0),new(x,h));
            for(var y=u*.1;y<h;y+=u*.1)dc.DrawLine(line,new(0,y),new(w,y));
            Text(dc,"QUEEN’S DOMAIN",u*.055,u*.05,u*.024,Mint);
            Text(dc,p.Approval.ToUpperInvariant()+"  /  TOUCH LAYOUT",u*.055,u*.085,u*.016,Muted);
            Text(dc,"Open swipe look",w*.62,h*.30,u*.025,Muted,true);
            Text(dc,"No edge spin · stops when your finger stops",w*.62,h*.35,u*.016,Muted,true);
            Text(dc,"Floating movement",w*.18,h*.67,u*.023,Muted,true);
            Text(dc,"Anchors where you touch",w*.18,h*.72,u*.016,Muted,true);
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(40,255,255,255)),1),new(w*.5-u*.018,h*.5),new(w*.5+u*.018,h*.5));
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(40,255,255,255)),1),new(w*.5,h*.5-u*.018),new(w*.5,h*.5+u*.018));
        }
        if(!Preview)
        {
            var header=$"{p.Approval.ToUpperInvariant()}  •  {(v.MenuMode?"MANUAL MENU":"GAMEPLAY")}  •  {(v.Focused?DriverStatus:"Waiting for game focus")}";
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(160,10,15,13)),null,new Rect(8,8,Math.Min(w-u*.15,u*.85),u*.036),4,4);
            Text(dc,header,15,10,u*.014,Mint);
        }
        if(v.Hidden)
        {dc.DrawRoundedRectangle(Ink,new Pen(Mint,1),new Rect(w-u*.13,0,u*.13,u*.12),5,5);Icons.Draw(dc,"radial",new(w-u*.065,u*.044),u*.036,White);Text(dc,"Show",w-u*.065,u*.075,u*.016,White,true);return;}
        if(DebugTargets)
        {
            foreach(var (z,label) in new[]{(p.MoveZone,"MOVE"),(p.LookZone,"LOOK")})
            {var pen=new Pen(new SolidColorBrush(Color.FromArgb(100,141,204,176)),1){DashStyle=DashStyles.Dash};dc.DrawRectangle(null,pen,new Rect(z.X*w,z.Y*h,z.Width*w,z.Height*h));Text(dc,label,z.X*w+8,z.Y*h+6,12,Mint);}
        }
        foreach(var c in Router.ActiveControls())
        {
            if(!c.Visible&&!DebugTargets)continue;
            var binding=Router.Resolve(c.Action);var icon=binding?.Icon??(c.Action=="actions"?"radial":"settings");
            var center=new Point(c.X*w,c.Y*h);var r=c.Size*u/2;
            var pressed=binding is not null&&(binding.Output switch{"RT"=>v.State.RT>0,"LT"=>v.State.LT>0,_=>(v.State.Buttons&ContactRouter.Mask(binding.Output))!=0});
            dc.PushOpacity(p.Opacity*c.Opacity*(c.Visible?1:.3));
            if(DebugTargets)dc.DrawEllipse(null,new Pen(Mint,1){DashStyle=DashStyles.Dot},center,r*c.HitScale,r*c.HitScale);
            dc.DrawEllipse(pressed?new SolidColorBrush(Color.FromArgb(100,166,218,193)):Fill,new Pen(c.Id==SelectedId?Mint:White,c.Action=="primary"?2:1.25),center,r,r);
            Icons.Draw(dc,icon,center,r*1.10,pressed?Mint:White);
            if(Preview||c.Label||DebugTargets)Text(dc,binding?.Label??"Actions",center.X,center.Y+r+u*.009,u*.014,White,true);
            dc.Pop();
        }
        if(v.MoveAnchor is Vec anchor&&v.MovePosition is Vec position)
        {
            var radius=p.MoveRadius*u;var d=position-anchor;if(d.Length>radius)d=d*(radius/d.Length);
            dc.DrawEllipse(Fill,new Pen(Mint,1),new(anchor.X,anchor.Y),radius,radius);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(85,166,218,193)),new Pen(Mint,1),new(anchor.X+d.X,anchor.Y+d.Y),u*.025,u*.025);
        }
        if(v.Panel.Length>0)DrawPanel(dc,v,p,w,h,u);
        if(DebugTargets)Text(dc,$"{v.Contacts} contacts  LS {v.State.LX:F2},{v.State.LY:F2}  RS {v.State.RX:F2},{v.State.RY:F2}  LT {v.State.LT} RT {v.State.RT}  {GyroStatus}",12,h-26,12,Mint);
    }
    private void DrawPanel(DrawingContext dc,RouterView v,GameProfile p,double w,double h,double u)
    {
        var center=Router.RadialCenter;var entries=Router.PanelEntries;
        if(v.Panel=="radial")
        {
            var r=p.RadialRadius*u;
            dc.DrawEllipse(Ink,new Pen(new SolidColorBrush(Color.FromArgb(80,166,218,193)),1),new(center.X,center.Y),r*1.44,r*1.44);
            for(var i=0;i<entries.Count;i++)
            {
                var angle=i*Math.PI*2/entries.Count;var x=center.X+Math.Sin(angle)*r;var y=center.Y-Math.Cos(angle)*r;
                var a=Router.Resolve(entries[i]);var label=a?.Label??Label(entries[i]);if(entries[i]=="gyro"&&!Preview)label=v.Gyro?"Gyro · ON":"Gyro · OFF";var icon=a?.Icon??(entries[i]=="gyro"?"gyro":"settings");
                var selected=i==v.Highlight;dc.DrawEllipse(selected?Mint:Fill,new Pen(selected?Mint:Muted,1),new(x,y),u*.041,u*.041);
                Icons.Draw(dc,icon,new(x,y),u*.040,selected?Ink:White);Text(dc,label,x,y+u*.047,u*.014,White,true);
            }
            Text(dc,"Cancel",center.X,center.Y-8,u*.018,Muted,true);
        }
        else
        {
            var width=u*.48;var row=u*.085;var x=Math.Clamp(center.X-width/2,8,w-width-8);var y=Math.Clamp(center.Y-entries.Count*row/2,8,h-entries.Count*row-8);
            dc.DrawRoundedRectangle(Ink,new Pen(Muted,1),new Rect(x,y,width,entries.Count*row),8,8);
            if(v.Panel.StartsWith("gyro"))
            {
                dc.DrawRoundedRectangle(Ink,new Pen(Mint,1),new Rect(x,y-u*.14,width,u*.13),8,8);
                Text(dc,"GYRO CALIBRATION",x+width/2,y-u*.125,u*.022,Mint,true);
                Text(dc,v.Panel=="gyroWait"?"Keep still… sampling for 1.5 seconds":"Keep tablet still, then press Start",x+width/2,y-u*.072,u*.017,White,true);
            }
            for(var i=0;i<entries.Count;i++)
            {
                var id=entries[i];if(i==v.Highlight)dc.DrawRectangle(Fill,null,new Rect(x,y+i*row,width,row));
                var a=Router.Resolve(id);var label=a?.Label??Label(id);if(v.Panel=="hold"&&i==0)label="HOLD  "+label;
                Icons.Draw(dc,a?.Icon??"settings",new(x+row*.48,y+(i+.5)*row),row*.40,White);
                Text(dc,label,x+row*.9,y+(i+.30)*row,u*.019,White);
            }
        }
    }
    internal static string Label(string id)=>id switch{"equipment"=>"Equipment / cycling","gyro"=>"Gyro on / off","gyroStart"=>"Start stationary calibration","more"=>"More","customize"=>"Customize Controls","profiles"=>"Choose profile","menu"=>"Toggle menu mode","hide"=>"Hide controls","exit"=>"Exit GlassPad","cancel"=>"Cancel",_=>id};
}
