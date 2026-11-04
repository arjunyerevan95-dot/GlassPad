// SPDX-License-Identifier: GPL-3.0-or-later
using GlassPad.Engine;
using System.Diagnostics;

var passed=0;
void Check(bool condition,string message){if(!condition)throw new Exception(message);passed++;}
void Test(string name,Action action){action();Console.WriteLine("PASS "+name);}
ContactRouter Router(GameProfile? p=null){var r=new ContactRouter(p??GameProfile.QueensDraft());r.SetSize(1280,800);r.SetFocus(true);return r;}
void Send(ContactRouter r,int id,TouchPhase phase,double x,double y,double time,double? received=null,double? now=null)=>r.Process(new(id,phase,new(x,y),time,received??time),now??time);
void Down(ContactRouter r,int id,double x,double y,double t)=>Send(r,id,TouchPhase.Down,x,y,t);
void Move(ContactRouter r,int id,double x,double y,double t)=>Send(r,id,TouchPhase.Move,x,y,t);
void Up(ContactRouter r,int id,double x,double y,double t)=>Send(r,id,TouchPhase.Up,x,y,t);
Vec Center(GameProfile p,string id){var c=p.Controls.First(x=>x.Id==id);return new(c.X*1280,c.Y*800);}
if(args.Length==2&&args[0]=="--write-default"){ProfileStore.SaveAtomic(args[1],GameProfile.QueensDraft());Console.WriteLine(args[1]);return;}
Test("floating stick starts neutral at any edge, stable owner, radial clamp, re-anchor",()=>{
    foreach(var origin in new[]{new Vec(28,240),new Vec(550,750),new Vec(250,600)})
    {
        var r=Router();Down(r,1,origin.X,origin.Y,1);Check(r.Sample(1).Neutral,"initial jump");
        Down(r,2,origin.X+20,origin.Y+20,1);Move(r,2,origin.X+100,origin.Y+100,1.01);Check(r.Sample(1.01).Neutral,"second contact stole movement");
        Move(r,1,origin.X+200,origin.Y+200,1.02);var s=r.Sample(1.02);Check(Math.Abs(Math.Sqrt(s.LX*s.LX+s.LY*s.LY)-1)<1e-9,"diagonal exceeds circle");
        Down(r,1,origin.X+60,origin.Y+60,1.025);Check(r.View(1.025).MoveAnchor==origin,"duplicate down recentered");
        Up(r,1,origin.X+200,origin.Y+200,1.03);Check(r.Sample(1.03).Neutral,"release held movement");
        Down(r,3,origin.X+5,origin.Y+5,1.04);Check(r.Sample(1.04).Neutral,"re-anchor not neutral");
    }
});
Test("atomic simultaneous move + look + attack + jump/dash; same-action owners",()=>{
    var p=GameProfile.QueensDraft();var c=Center(p,"primary");var jump=Center(p,"jump");var dash=Center(p,"dash");
    var r=Router(p);
    r.ProcessBatch([new(1,TouchPhase.Down,new(200,600),1,1),new(2,TouchPhase.Down,new(800,300),1,1),new(3,TouchPhase.Down,c,1,1),new(4,TouchPhase.Down,jump,1,1),new(5,TouchPhase.Down,dash,1,1)],1);
    Move(r,1,270,600,1.016);Move(r,2,820,300,1.016);var s=r.Sample(1.017);
    Check(s.LX>.5&&s.RX>0&&s.RT==255&&(s.Buttons&0x1200)==0x1200,"simultaneous batch dropped a role");
    Down(r,6,c.X,c.Y,1.02);Up(r,3,c.X,c.Y,1.025);Check(r.Sample(1.025).RT==255,"release cancelled second same-action owner");
    Up(r,6,c.X,c.Y,1.03);Check(r.Sample(1.03).RT==0,"final action owner did not release");
    Move(r,2,c.X,c.Y,1.04);Check(r.Sample(1.04).RT==0,"look crossing button fired attack");
    r.Clear();Check(r.Sample(2).Neutral,"clear left input");
});
Test("attack hold and release while looking; generic explicit toggle/slide",()=>{
    var p=GameProfile.QueensDraft();var r=Router(p);var a=Center(p,"secondary");Down(r,1,a.X,a.Y,1);Move(r,1,a.X+10,a.Y-5,1.016);
    Check(r.Sample(1.017).LT==255&&r.Sample(1.017).RX>0,"charged secondary drag lost hold or look");
    Check(r.Sample(1.1).LT==255&&r.Sample(1.1).RX==0,"stationary action lost hold or kept look");Up(r,1,a.X+10,a.Y-5,1.11);Check(r.Sample(1.11).Neutral,"secondary release stuck");
    p.Binding("secondary")!.Interaction="AimToggleSlide";p.Binding("secondary")!.SlideAction="primary";r=Router(p);Down(r,1,a.X,a.Y,1);Move(r,1,a.X+40,a.Y,1.016);Check(r.Sample(1.017).LT==255&&r.Sample(1.017).RT==255,"generic slide-to-fire missing");Up(r,1,a.X+40,a.Y,1.02);Check(r.Sample(1.02).LT==255&&r.Sample(1.02).RT==0,"slide fire leaked across release");r.SetFocus(false);Check(r.Sample(1.03).Neutral,"latched action survived focus loss");
});
Test("swipe rate/delivery/frame phase matrix; no backlog after stalls",()=>{
    var c=new LookCalibration();
    foreach(var sourceHz in new[]{30.0,60,120,240})foreach(var batchHz in new[]{30.0,60,120}.Where(h=>h<=sourceHz))foreach(var fps in new[]{15.0,30,60})
    {
        var aim=new SwipeAim();aim.Begin(new(0,0),1000,10);var last=10.0;var position=0.0;
        for(int i=1;i<=30;i++){var source=Math.Floor(i/batchHz*sourceHz+1e-8)/sourceHz;var delivery=10+i/batchHz;position=source*200;aim.Move(new(position,0),1000+source,delivery,c);Check(aim.Sample(delivery+.001).X>0,"delivered sweep expired");last=delivery;}
        Check(aim.Sample(last+.060)==default,"stationary aim stuck");
        for(double phase=0;phase<.004;phase+=.0013){double neutral=0;for(int tick=0;tick<30;tick++){var t=last+phase+tick*.004;if(aim.Sample(t)==default){neutral=t-last;break;}}Check(neutral>0&&neutral<=.0501,"watchdog deadline");var gameObservation=Math.Ceiling((neutral+.0001)*fps)/fps;Check(gameObservation<=.0501+1/fps,"low FPS neutral bound");}
        aim.Move(new(position+.1,0),1002,12,c);Check(aim.Sample(12.001).X>0,"slow correction after stationary gap lost");
        aim.Move(new(position+10000,0),1002.016,12.016,c);Check(aim.Sample(12.017).X==1,"not saturated");
        aim.Move(new(position+9999,0),1002.032,12.032,c);Check(aim.Sample(12.033).X<0,"queued camera catch-up");
        aim.Move(new(1e9,0),1002.020,12.034,c);Check(aim.Sample(12.035).X<0,"out-of-order source overwrote motion");
        aim.Move(new(1e9,0),1003,12.030,c);Check(aim.Sample(12.035).X<0,"out-of-order delivery overwrote motion");
        Check(aim.Sample(12.2)==default,"stale sample renewed itself");
    }
    var r=Router();Down(r,1,800,300,1);Send(r,1,TouchPhase.Move,900,300,1.016,1.016,1.3);Check(r.Sample(1.3).Neutral,"stalled input replayed");
});
Test("radial centre cancellation, drag release, tap-open, hold popup",()=>{
    var p=GameProfile.QueensDraft();var r=Router(p);var a=Center(p,"actions");
    Down(r,1,a.X,a.Y,1);var center=r.RadialCenter;Move(r,1,center.X,center.Y,1.02);Up(r,1,center.X,center.Y,1.04);Check(r.Sample(1.04).Neutral&&r.View(1.04).Panel=="","cancel fired highlighted action");
    Down(r,2,a.X,a.Y,2);Up(r,2,a.X,a.Y,2.03);Check(r.View(2.03).Panel=="radial","tap open failed");
    var top=new Vec(center.X,center.Y-p.RadialRadius*800);Down(r,3,top.X,top.Y,2.1);Up(r,3,top.X,top.Y,2.13);Check(r.Sample(2.13).Neutral&&r.View(2.13).Panel=="hold","hold action executed during radial selection");
    var holdPoint=new Vec(center.X,center.Y-.085*800*.5);Down(r,4,holdPoint.X,holdPoint.Y,2.2);Check((r.Sample(2.2).Buttons&0x8000)!=0,"special hold unavailable");Move(r,4,holdPoint.X+8,holdPoint.Y,2.216);Check(r.Sample(2.217).RX>0,"radial hold cannot drag look");Up(r,4,holdPoint.X+8,holdPoint.Y,2.23);Check(r.Sample(2.23).Neutral,"radial hold release stuck");
    r.ShowPanel("radial");Down(r,5,top.X,top.Y,3);Send(r,5,TouchPhase.Cancel,top.X,top.Y,3.01);Check(r.View(3.01).Panel==""&&r.Sample(3.01).Neutral,"cancelled menu executed");
});
Test("tap-run single pulse, deliberate hold-run hysteresis",()=>{
    var p=GameProfile.QueensDraft();p.Sprint="DoubleTap";var r=Router(p);Down(r,1,200,600,1);Up(r,1,200,600,1.06);Down(r,2,202,600,1.18);Check((r.Sample(1.18).Buttons&0x40)!=0,"double tap did not request run");Move(r,2,350,600,1.2);Check((r.Sample(1.4).Buttons&0x40)==0,"run pulsed every frame");
    p.Sprint="OuterHold";r=Router(p);Down(r,1,200,600,2);Move(r,1,300,600,2.02);Check((r.Sample(2.02).Buttons&0x40)!=0,"outer hold missing");Move(r,1,275,600,2.04);Check((r.Sample(2.04).Buttons&0x40)!=0,"hysteresis missing");Move(r,1,220,600,2.06);Check((r.Sample(2.06).Buttons&0x40)==0,"inner threshold failed");
});
Test("focus/hide/profile/menu/editor/power/shutdown cancellation",()=>{
    var p=GameProfile.QueensDraft();foreach(var cancel in new Action<ContactRouter>[] {r=>r.SetFocus(false),r=>r.ToggleHidden(),r=>r.Configure(p.Clone()),r=>r.ToggleMenu(),r=>r.SetEnabled(false),r=>r.Clear("suspend"),r=>r.Clear("shutdown")})
    {var r=Router(p);var a=Center(p,"primary");Down(r,1,a.X,a.Y,1);Down(r,2,200,600,1);Move(r,2,280,600,1.016);Check(!r.Sample(1.02).Neutral,"fixture not active");cancel(r);Check(r.Sample(1.03).Neutral,"transition left input held");Move(r,1,a.X+50,a.Y,1.04);Check(r.Sample(1.04).Neutral,"cancelled owner resumed");}
});
Test("profile validation, round trip, atomic save, cancellation and DPI normalization",()=>{
    var p=GameProfile.QueensDraft();var json=ProfileStore.Serialize(p);Check(ProfileStore.Serialize(ProfileStore.Parse(json))==json,"round trip changed profile");
    var working=p.Clone();working.Controls[0].X=.70;Check(p.Controls[0].X!=working.Controls[0].X,"editor draft aliases original");
    foreach(var bad in new[]{"version","offscreen","unknown","deadzone","radial"}){var b=p.Clone();switch(bad){case "version":b.Version=9;break;case "offscreen":b.Controls[0].X=1.5;break;case "unknown":b.Controls[0].Action="unknown";break;case "deadzone":b.MoveDeadzone=double.NaN;break;case "radial":b.Radial.Add("invalid");break;}Check(ProfileStore.Validate(b).Count>0,"invalid profile accepted: "+bad);}
    var temp=Path.Combine(Path.GetTempPath(),"glasspad-tests-"+Guid.NewGuid()+".json");try{ProfileStore.SaveAtomic(temp,p);ProfileStore.SaveAtomic(temp,working);Check(ProfileStore.Parse(File.ReadAllText(temp)).Controls[0].X==.70,"save not persisted");Check(ProfileStore.Parse(File.ReadAllText(temp+".bak")).Controls[0].X==p.Controls[0].X,"backup missing");}finally{File.Delete(temp);File.Delete(temp+".bak");}
    foreach(var scale in new[]{1.0,1.25,1.5,2.0,2.25}){var r=Router(p);r.SetSize(1280*scale,800*scale);Down(r,1,200*scale,600*scale,1);Move(r,1,260*scale,600*scale,1.016);Check(Math.Abs(r.Sample(1.017).LX-.6135265700483092)<.001,"DPI scaled movement changed");}
});
Test("other fingers' stationary frame samples do not cancel swipe look",()=>{
    var r=Router();Down(r,1,200,600,1);Down(r,2,800,300,1);
    r.ProcessBatch([new(1,TouchPhase.Move,new(260,600),1.016,1.016),new(2,TouchPhase.Move,new(815,300),1.016,1.016)],1.016);
    Check(r.Sample(1.017).RX>0,"initial swipe missing");
    for(int i=1;i<=5;i++)
    {
        var time=1.016+i*.004;
        r.ProcessBatch([new(1,TouchPhase.Move,new(260+i,600),time,time),new(2,TouchPhase.Move,new(815,300),time,time)],time);
        Check(r.Sample(time+.001).RX>0,"stationary look samples in another finger's frame cancelled fresh swipe");
    }
    Check(r.Sample(1.080).RX==0,"stationary frame samples renewed swipe expiry");
});
Test("gyro bias/deadzone/axis/inversion, composition, expiry and menu gating",()=>{
    var p=GameProfile.QueensDraft();p.Gyro.BiasX=.2;p.Gyro.BiasY=-.8;
    Check(GyroMath.Map(-.8,.2,0,p.Gyro,p.Look)==default,"stationary bias retained gyro motion");
    var value=GyroMath.Map(-.8,90.7,0,p.Gyro,p.Look);Check(value.X==1&&value.Y==0,"yaw did not saturate");
    p.Gyro.InvertX=true;Check(GyroMath.Map(-.8,45,0,p.Gyro,p.Look).X<0,"gyro invert missing");
    p.Gyro.YawAxis="Z";Check(GyroMath.Map(-.8,0,45,p.Gyro,p.Look).X<0,"axis selection missing");
    var r=Router(p);r.SetGyro(new(.8,.8),1);Check(r.Sample(1).Neutral,"gyro on by default");r.SetGyroEnabled(true);r.SetGyro(new(.8,.8),1);Check(Math.Abs(new Vec(r.Sample(1.001).RX,r.Sample(1.001).RY).Length-1)<1e-9,"gyro diagonal unclamped");
    Down(r,1,800,300,1);Move(r,1,820,300,1.016);Check(r.Sample(1.017).RX>.7,"touch and gyro competed");Check(r.Sample(1.08).Neutral,"gyro or touch stale output persisted");
    r.SetGyro(new(.5,.1),2);r.ToggleMenu();Check(r.Sample(2).Neutral,"menu retained gyro");r.ToggleMenu();r.SetFocus(false);Check(r.Sample(2).Neutral,"foreground loss retained gyro");
});
Console.WriteLine($"PASS {passed} assertions; all engine suites complete");
