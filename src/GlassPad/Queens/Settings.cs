// SPDX-License-Identifier: GPL-3.0-or-later
using System.IO;
using GlassPad.Engine;
namespace GlassPad.Queens;
internal static class Settings
{
    public const string Build="1.1.0-R3";
    internal static string? TestRoot { get; set; }
    public static string Root=>TestRoot??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"GlassPad-Queens-Pilot");
    public static string ProfilePath=>Path.Combine(Root,"profiles","queens-domain-approved.json");
    public static string PathFor(GameProfile p)=>Path.Combine(Root,"profiles",p.Id+".json");
    public static void Save(GameProfile p)
    {
        ProfileStore.SaveAtomic(PathFor(p),p);
        if(p.Approval=="Approved"){var selected=Path.Combine(Root,"selected-profile.txt");File.WriteAllText(selected+".tmp",p.Id);File.Move(selected+".tmp",selected,true);}
    }
    public static List<GameProfile> ApprovedProfiles()
    {
        var profiles=new Dictionary<string,GameProfile>();
        foreach(var dir in new[]{Path.Combine(AppContext.BaseDirectory,"profiles"),Path.Combine(Root,"profiles")})
        {if(!Directory.Exists(dir))continue;foreach(var path in Directory.EnumerateFiles(dir,"*.json")){try{var p=ProfileStore.Parse(File.ReadAllText(path));if(p.Approval=="Approved")profiles[p.Id]=p;}catch{}}}
        return profiles.Values.ToList();
    }
    public static GameProfile Load()
    {
        try
        {
            var marker=Path.Combine(Root,"selected-profile.txt");if(File.Exists(marker))
            {var id=File.ReadAllText(marker);if(id.Length is >0 and <=80&&id.All(c=>char.IsAsciiLetterOrDigit(c)||c is '-' or '_')){var path=Path.Combine(Root,"profiles",id+".json");if(File.Exists(path))return ProfileStore.Parse(File.ReadAllText(path));}}
            if(File.Exists(ProfilePath))return ProfileStore.Parse(File.ReadAllText(ProfilePath));
        }
        catch(Exception ex){Directory.CreateDirectory(Root);File.AppendAllText(Path.Combine(Root,"profile-errors.log"),DateTimeOffset.Now+" "+ex.Message+"\n");}
        var shipped=Path.Combine(AppContext.BaseDirectory,"profiles","queens-domain-approved.json");
        return File.Exists(shipped)?ProfileStore.Parse(File.ReadAllText(shipped)):GameProfile.QueensDraft();
    }
}
