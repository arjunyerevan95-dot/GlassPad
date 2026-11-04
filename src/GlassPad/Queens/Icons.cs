// SPDX-License-Identifier: GPL-3.0-or-later
// Original flat vector drawings for this Windows fork; no SF Symbols assets.
using System.Windows;
using System.Windows.Media;
namespace GlassPad.Queens;
internal static class Icons
{
    private static readonly Dictionary<string,Geometry> Cache=[];
    private static string Path(string id)=>id switch{
        "sword"=>"M5,25 L9,21 M7,17 L15,25 M11,21 L27,5 L23,5 L7,21 M15,17 L23,9",
        "secondary"=>"M5,22 Q6,7 24,5 L20,10 M6,17 L16,27 M10,22 L25,7 M23,17 L27,21",
        "jump"=>"M16,27 L16,6 M8,14 L16,6 L24,14 M6,27 L10,27 M22,27 L26,27",
        "dash"=>"M4,12 L16,12 L12,5 L28,16 L12,27 L16,20 L4,20 M3,16 L11,16",
        "hand"=>"M9,27 L5,17 Q5,13 8,15 L11,18 L11,6 Q12,3 14,6 L14,14 L14,4 Q16,1 18,4 L18,14 L18,6 Q20,3 22,6 L22,15 L22,10 Q25,7 26,11 L26,22 L22,28 Z",
        "throw"=>"M5,26 L23,8 M18,8 L27,5 L24,14 M10,7 L10,11 M5,11 L8,14 M21,23 L26,23",
        "item"=>"M12,4 L20,4 L20,10 L25,17 L25,27 L7,27 L7,17 L12,10 Z M9,19 L23,19 M13,23 L19,23",
        "status"=>"M6,6 L13,5 L16,8 L19,5 L26,6 L26,26 L19,25 L16,28 L13,25 L6,26 Z M16,8 L16,28",
        "radial"=>"M16,3 A13,13 0 1 0 16,29 A13,13 0 1 0 16,3 M16,3 L16,10 M27,10 L21,13 M27,23 L21,19 M16,29 L16,22 M5,23 L11,19 M5,10 L11,13",
        "pause"=>"M11,7 L11,25 M21,7 L21,25",
        "special"=>"M18,3 L6,18 L14,18 L12,29 L26,12 L18,12 Z",
        "run"=>"M17,4 A3,3 0 1 0 17,10 A3,3 0 1 0 17,4 M16,12 L11,20 L17,25 L14,30 M14,16 L22,18 L27,14 M13,20 L5,26 M10,12 L5,17",
        "gyro"=>"M7,9 A12,12 0 0 1 28,17 M28,17 L29,10 M28,17 L21,14 M25,23 A12,12 0 0 1 4,15 M4,15 L3,22 M4,15 L11,18 M16,11 L16,21 M11,16 L21,16",
        "up"=>"M6,22 L16,10 L26,22","down"=>"M6,10 L16,22 L26,10","left"=>"M22,6 L10,16 L22,26","right"=>"M10,6 L22,16 L10,26",
        "confirm"=>"M6,16 L13,23 L27,8","back"=>"M9,9 L23,23 M9,23 L23,9",
        _=>"M16,4 L16,10 M16,22 L16,28 M4,16 L10,16 M22,16 L28,16 M8,8 L12,12 M20,20 L24,24 M8,24 L12,20 M20,12 L24,8 M16,10 A6,6 0 1 0 16,22 A6,6 0 1 0 16,10"
    };
    internal static void Draw(DrawingContext dc,string id,Point center,double size,Brush brush)
    {
        if(!Cache.TryGetValue(id,out var geometry)){geometry=Geometry.Parse(Path(id));geometry.Freeze();Cache[id]=geometry;}
        dc.PushTransform(new TranslateTransform(center.X-size/2,center.Y-size/2));dc.PushTransform(new ScaleTransform(size/32,size/32));
        dc.DrawGeometry(null,new Pen(brush,1.8){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round,LineJoin=PenLineJoin.Round},geometry);dc.Pop();dc.Pop();
    }
}
