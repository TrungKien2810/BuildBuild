param([switch]$PreviewOnly)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$code = @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Text;

public static class SonicExport {

    static readonly Color[] Palette = MakePalette();
    static readonly Dictionary<int,Color> Cache = new Dictionary<int,Color>();
    static Color[] MakePalette() {
        string[] hex={"171C30","252C40","394052","535C6D","737D8E","A0A9B6","352323","523124","75432B","965A32","B97943","D59B5D","674D2E","896333","B38640","D4AA54","F1CE77","F6DCA0","373439","514C4D","686264","817A75","9F9789","B9B09C","D2C9AE","751F2B","AB2933","D6433B","F16A4C","FF6E60","FFD0BD","FFF4D6","D4D4CC","B2B5B5","90989E","617D91","8EADBE","B8CED7","DDEAF0","F5FAF6"};
        Color[] colors=new Color[hex.Length]; for(int i=0;i<hex.Length;i++) colors[i]=ColorTranslator.FromHtml("#"+hex[i]); return colors;
    }
    static Color Quantize(Color c) {
        if(c.A<200) return Color.Transparent;
        int key=c.ToArgb(); Color result; if(Cache.TryGetValue(key,out result)) return result;
        int dist=int.MaxValue; result=Palette[0];
        foreach(Color p in Palette) { int r=c.R-p.R,g=c.G-p.G,b=c.B-p.B; int d=r*r+g*g+b*b; if(d<dist){dist=d;result=p;} }
        Cache[key]=result; return result;
    }
    public static Rectangle Bounds(Bitmap b) {
        int l=b.Width,t=b.Height,r=-1,bot=-1;
        for(int y=0;y<b.Height;y++) for(int x=0;x<b.Width;x++) if(b.GetPixel(x,y).A>=200){l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);bot=Math.Max(bot,y);}
        if(r<0) throw new Exception("Empty frame"); return Rectangle.FromLTRB(l,t,r+1,bot+1);
    }
    static Bitmap[] Cells(string path,int cols,int rows,int count) {
        Bitmap[] result=new Bitmap[count]; using(Bitmap s=new Bitmap(path)) {
            for(int i=0;i<count;i++) {
                int x0=(i%cols)*s.Width/cols,x1=(i%cols+1)*s.Width/cols;
                int y0=(i/cols)*s.Height/rows,y1=(i/cols+1)*s.Height/rows;
                result[i]=s.Clone(new Rectangle(x0,y0,x1-x0,y1-y0),PixelFormat.Format32bppArgb);
            }
        } return result;
    }
    static Bitmap Sample(Bitmap src,Rectangle crop,int width,int height) {
        Bitmap b=new Bitmap(width,height,PixelFormat.Format32bppArgb);
        for(int y=0;y<height;y++) for(int x=0;x<width;x++) {
            int sx=crop.X+Math.Min(crop.Width-1,(int)((x+0.5)*crop.Width/width));
            int sy=crop.Y+Math.Min(crop.Height-1,(int)((y+0.5)*crop.Height/height));
            b.SetPixel(x,y,Quantize(src.GetPixel(sx,sy)));
        } return b;
    }
    static void Blit(Bitmap dst,Bitmap src,int dx,int dy) {
        for(int y=0;y<src.Height;y++) for(int x=0;x<src.Width;x++) {
            int tx=dx+x,ty=dy+y; if(tx<0||ty<0||tx>=dst.Width||ty>=dst.Height) continue;
            Color c=src.GetPixel(x,y); if(c.A>=200) dst.SetPixel(tx,ty,c);
        }
    }
    static void Patch(Bitmap dst,Bitmap src,Rectangle rect) {
        for(int y=rect.Top;y<rect.Bottom;y++) for(int x=rect.Left;x<rect.Right;x++) dst.SetPixel(x,y,src.GetPixel(x,y));
    }

    static void SaveGroup(string root,string dir,string prefix,Bitmap[] frames,int cols) {
        string folder=Path.Combine(root,dir); Directory.CreateDirectory(folder);
        int rows=(frames.Length+cols-1)/cols; using(Bitmap sheet=new Bitmap(frames[0].Width*cols,frames[0].Height*rows)) {
            for(int i=0;i<frames.Length;i++) {
                frames[i].Save(Path.Combine(folder,prefix+"-"+(i+1)+".png"),ImageFormat.Png);
                Blit(sheet,frames[i],(i%cols)*frames[i].Width,(i/cols)*frames[i].Height);
            }
            sheet.Save(Path.Combine(folder,"sheet-transparent.png"),ImageFormat.Png);
        }
    }
    static void Board(string root,Dictionary<string,Bitmap[]> groups) {
        using(Bitmap board=new Bitmap(1728,groups.Count*302)) using(Graphics g=Graphics.FromImage(board)) using(Font f=new Font("Arial",14)) {
            g.Clear(Color.FromArgb(224,224,224)); g.InterpolationMode=InterpolationMode.NearestNeighbor; g.PixelOffsetMode=PixelOffsetMode.Half;
            int row=0; foreach(KeyValuePair<string,Bitmap[]> group in groups) {
                g.DrawString(group.Key,f,Brushes.Black,12,row*302+8);
                for(int i=0;i<group.Value.Length;i++) {
                    Bitmap b=group.Value[i]; int scale=b.Width==48?4:2;
                    g.DrawImage(b,new Rectangle(i*288+(288-b.Width*scale)/2,row*302+32,b.Width*scale,b.Height*scale),0,0,b.Width,b.Height,GraphicsUnit.Pixel);
                    g.DrawString((i+1).ToString(),f,Brushes.Black,i*288+10,row*302+275);
                } row++;
            } board.Save(Path.Combine(root,"qc","all-assets.png"),ImageFormat.Png);
        }
    }
    static Bitmap Indexed(Bitmap src){
        Dictionary<int,int> counts=new Dictionary<int,int>();
        for(int y=0;y<src.Height;y++)for(int x=0;x<src.Width;x++){
            int key=src.GetPixel(x,y).ToArgb();int n;counts.TryGetValue(key,out n);counts[key]=n+1;
        }
        List<int> keys=new List<int>(counts.Keys);keys.Sort(delegate(int a,int b){return counts[b].CompareTo(counts[a]);});
        if(keys.Count>256)keys.RemoveRange(256,keys.Count-256);
        Bitmap result=new Bitmap(src.Width,src.Height,PixelFormat.Format8bppIndexed);
        ColorPalette palette=result.Palette;Dictionary<int,byte> map=new Dictionary<int,byte>();
        for(int i=0;i<keys.Count;i++){palette.Entries[i]=Color.FromArgb(keys[i]);map[keys[i]]=(byte)i;}
        result.Palette=palette;
        BitmapData data=result.LockBits(new Rectangle(0,0,result.Width,result.Height),ImageLockMode.WriteOnly,PixelFormat.Format8bppIndexed);
        byte[] bytes=new byte[data.Stride*result.Height];
        for(int y=0;y<src.Height;y++)for(int x=0;x<src.Width;x++){
            Color c=src.GetPixel(x,y);byte index;if(!map.TryGetValue(c.ToArgb(),out index)){
                int best=int.MaxValue;index=0;
                for(int i=0;i<keys.Count;i++){Color p=Color.FromArgb(keys[i]);int r=c.R-p.R,g=c.G-p.G,b=c.B-p.B,d=r*r+g*g+b*b;if(d<best){best=d;index=(byte)i;}}
                map[c.ToArgb()]=index;
            }
            bytes[y*data.Stride+x]=index;
        }
        System.Runtime.InteropServices.Marshal.Copy(bytes,0,data.Scan0,bytes.Length);result.UnlockBits(data);return result;
    }
    static void Gif(string path,List<Bitmap> frames,List<int> delays) {
        using(BinaryWriter writer=new BinaryWriter(File.Create(path))) {
            writer.Write(Encoding.ASCII.GetBytes("GIF89a")); writer.Write((ushort)frames[0].Width);writer.Write((ushort)frames[0].Height);
            writer.Write((byte)0x70);writer.Write((byte)0);writer.Write((byte)0);
            writer.Write(new byte[]{0x21,0xff,0x0b});writer.Write(Encoding.ASCII.GetBytes("NETSCAPE2.0"));writer.Write(new byte[]{3,1,0,0,0});
            for(int i=0;i<frames.Count;i++) {
                byte[] data;using(MemoryStream stream=new MemoryStream())using(Bitmap indexed=Indexed(frames[i])){indexed.Save(stream,ImageFormat.Gif);data=stream.ToArray();}
                byte packed=data[10];int paletteSize=3*(1<<((packed&7)+1));int pos=13+(((packed&128)!=0)?paletteSize:0);
                while(data[pos]!=0x2c) {
                    if(data[pos]!=0x21) throw new Exception("Unexpected GIF block");pos+=2;
                    while(data[pos]!=0){pos+=data[pos]+1;}pos++;
                }
                byte imagePacked=data[pos+9];int localSize=3*(1<<((imagePacked&7)+1));bool local=(imagePacked&128)!=0;
                byte[] palette=new byte[local?localSize:paletteSize];Array.Copy(data,local?pos+10:13,palette,0,palette.Length);
                writer.Write(new byte[]{0x21,0xf9,4,4});writer.Write((ushort)Math.Max(1,delays[i]/10));writer.Write((byte)0);writer.Write((byte)0);
                writer.Write((byte)0x2c);writer.Write((ushort)0);writer.Write((ushort)0);writer.Write((ushort)frames[i].Width);writer.Write((ushort)frames[i].Height);
                writer.Write((byte)(0x80|(imagePacked&0x40)|((local?imagePacked:packed)&7)));writer.Write(palette);
                pos+=10+(local?localSize:0);writer.Write(data[pos++]);
                while(data[pos]!=0){int size=data[pos];writer.Write(data,pos,size+1);pos+=size+1;}writer.Write((byte)0);
            }writer.Write((byte)0x3b);
        }
    }


    static Rectangle BodyBounds;
    const int BaseY=78;
    static readonly Point Mouth=new Point(32,50);
    static readonly Rectangle[] Membranes={new Rectangle(41,37,6,10),new Rectangle(41,61,6,10)};
    static readonly Rectangle[] Lenses={new Rectangle(79,56,5,7),new Rectangle(85,56,5,6),new Rectangle(91,55,4,6)};
    static Bitmap NormalizeBody(Bitmap src){
        Bitmap b=new Bitmap(128,128);using(Bitmap fit=Sample(src,Bounds(src),BodyBounds.Width,BodyBounds.Height))Blit(b,fit,BodyBounds.X,BodyBounds.Y);return b;
    }
    static void MembranePatch(Bitmap dst,Bitmap model,Bitmap identity,int which){
        Rectangle r=Membranes[which];
        for(int y=r.Top;y<r.Bottom;y++)for(int x=r.Left;x<r.Right;x++){
            Color a=identity.GetPixel(x,y),c=model.GetPixel(x,y);
            if(a.A>0&&a.B>=a.R&&Math.Abs(a.R-a.G)<25&&c.A>0)dst.SetPixel(x,y,c);
        }
    }
    static int Offset(string group,int frame){
        return group=="attack-shock"?(frame==3?2:frame==4?1:0):group=="attack"&&frame==3?1:0;
    }
    static Bitmap Shift(Bitmap src,int offset){
        Bitmap b=new Bitmap(128,128);
        for(int y=0;y<128;y++)for(int x=0;x<128;x++){
            Color c=src.GetPixel(x,y);int xx=x+(y<BaseY?offset:0);if(c.A>0&&xx<128)b.SetPixel(xx,y,c);
        }return b;
    }
    static Bitmap[] Bodies(string root,string group,Bitmap identity){
        int n=group=="idle"?4:6;
        Bitmap[] cells=Cells(Path.Combine(root,"raw",group+"-sheet.png"),n==4?2:3,2,n);
        Bitmap[] frames=new Bitmap[n];for(int i=0;i<n;i++)frames[i]=(Bitmap)identity.Clone();
        if(n==4){
            for(int i=1;i<3;i++)using(Bitmap model=NormalizeBody(cells[i]))MembranePatch(frames[i],model,identity,i-1);
        }else{
            for(int i=1;i<3;i++)using(Bitmap model=NormalizeBody(cells[i]))for(int m=0;m<2;m++)MembranePatch(frames[i],model,identity,m);
            for(int i=3;i<5;i++){frames[i].Dispose();frames[i]=Shift(identity,Offset(group,i));}
        }
        foreach(Bitmap b in cells)b.Dispose();return frames;
    }
    static Bitmap[] Lamps(Bitmap identity,Bitmap ready){
        Bitmap[] states=new Bitmap[4];
        for(int count=0;count<4;count++){
            states[count]=new Bitmap(128,128);
            for(int lamp=0;lamp<3;lamp++)Patch(states[count],lamp<count?ready:identity,Lenses[lamp]);
        }return states;
    }
    static Bitmap[] Rhythms(Bitmap identity,Bitmap[] lamps){
        Bitmap[] states=new Bitmap[4];
        for(int i=0;i<4;i++){states[i]=(Bitmap)identity.Clone();Blit(states[i],lamps[i],0,0);}return states;
    }
    static Bitmap[] Effects(string root,string group,int n,int maxW,int maxH,bool wave){
        Bitmap[] cells=Cells(Path.Combine(root,"raw",group+"-sheet.png"),n==6?3:2,2,n);
        int bw=0,bh=0;foreach(Bitmap cell in cells){Rectangle r=Bounds(cell);bw=Math.Max(bw,r.Width);bh=Math.Max(bh,r.Height);}
        Bitmap[] frames=new Bitmap[n];
        for(int i=0;i<n;i++){
            Rectangle r=Bounds(cells[i]);int w=Math.Max(1,(int)Math.Round((double)r.Width/bw*maxW)),h=Math.Max(1,(int)Math.Round((double)r.Height/bh*maxH));
            frames[i]=new Bitmap(128,128);using(Bitmap fit=Sample(cells[i],r,w,h))Blit(frames[i],fit,wave?112-w:64-w/2,64-h/2);cells[i].Dispose();
        }return frames;
    }
    static Bitmap Scene(Bitmap body,Bitmap lamp,int shift,Bitmap wave,Bitmap hit,Bitmap enemy,int shot,int count,int push){
        Bitmap small=new Bitmap(448,180);
        using(Graphics g=Graphics.FromImage(small))using(Font f=new Font("Arial",9)){
            g.Clear(Color.FromArgb(211,211,211));
            g.DrawString("SONIC B / SR",f,Brushes.DarkSlateGray,10,10);
            g.DrawString(shot==0?"IDLE":shot==4?"SHOT 4: SHOCK / 35 + PUSH":"SHOT "+shot+": NORMAL / 20",f,Brushes.DarkSlateGray,10,26);
            g.DrawString("LAMPS "+count+"/3",f,Brushes.DarkSlateGray,330,10);
        }
        int[] xs={245,270,295};
        foreach(int x in xs)Blit(small,enemy,x-push,66);
        Blit(small,body,300,25);Blit(small,lamp,300+shift,25);
        if(wave!=null)Blit(small,wave,300+Mouth.X+shift-112,25+Mouth.Y-64);
        if(hit!=null)foreach(int x in xs)Blit(small,hit,x+10-push-64,78-64);
        Bitmap large=new Bitmap(1344,540);using(Graphics g=Graphics.FromImage(large)){
            g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
            g.DrawImage(small,new Rectangle(0,0,1344,540),0,0,448,180,GraphicsUnit.Pixel);
        }small.Dispose();return large;
    }
    static void Preview(string root,Dictionary<string,Bitmap[]> groups,bool writeClips=true){
        List<Bitmap> frames=new List<Bitmap>();List<int> times=new List<int>();
        string path=Path.GetFullPath(Path.Combine(root,"..","..","Assets","Art","Generated","RatRunner","identity","identity-1.png"));
        using(Bitmap raw=new Bitmap(path))using(Bitmap enemy=Sample(raw,Bounds(raw),20,24)){
            Bitmap[] idle=groups["idle"],lamps=groups["lamp-overlay"];
            for(int loop=0;loop<2;loop++)for(int i=0;i<4;i++){frames.Add(Scene(idle[i],lamps[0],0,null,null,enemy,0,0,0));times.Add(120);}
            int[] cadence={100,120,90,100,100,90};
            for(int shot=1;shot<=4;shot++){
                bool strong=shot==4;string attack=strong?"attack-shock":"attack";int before=shot-1,after=strong?0:shot;
                for(int i=0;i<6;i++){
                    Bitmap wave=i>=2?groups[strong?"wave-shock":"wave-normal"][i-2]:null;
                    Bitmap hit=i>=2?groups[strong?"hit-shock":"hit-normal"][i-2]:null;
                    int push=strong&&i>=2?Math.Min(10,(i-1)*3):0;
                    frames.Add(Scene(groups[attack][i],lamps[i<2?before:after],Offset(attack,i),wave,hit,enemy,shot,i<2?before:after,push));times.Add(cadence[i]);
                }
                for(int i=0;i<4;i++){
                    Bitmap wave=strong&&i<2?groups["wave-shock"][i+4]:null;
                    frames.Add(Scene(idle[i],lamps[after],0,wave,null,enemy,shot,after,strong?10:0));times.Add(100);
                }
            }
            frames.Add(Scene(idle[0],lamps[0],0,null,null,enemy,0,0,0));times.Add(700);
        }
        string file=Path.Combine(root,"qc","preview.gif");Gif(file,frames,times);
        using(Bitmap gif=new Bitmap(file)){
            int n=gif.GetFrameCount(FrameDimension.Time);if(n!=frames.Count)throw new Exception("GIF count");
            for(int i=0;i<n;i++)gif.SelectActiveFrame(FrameDimension.Time,i);
        }
        frames[30].Save(Path.Combine(root,"qc","preview-3-lamps.png"),ImageFormat.Png);
        frames[43].Save(Path.Combine(root,"qc","preview-shock.png"),ImageFormat.Png);
        foreach(Bitmap b in frames)b.Dispose();
        if(!writeClips)return;
        Directory.CreateDirectory(Path.Combine(root,"qc","clips"));
        foreach(KeyValuePair<string,Bitmap[]> group in groups){
            if(group.Key=="icon")continue;
            List<Bitmap> clip=new List<Bitmap>();List<int> delay=new List<int>();
            foreach(Bitmap b in group.Value){
                Bitmap canvas=new Bitmap(384,384);using(Graphics g=Graphics.FromImage(canvas)){
                    g.Clear(Color.FromArgb(211,211,211));g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
                    g.DrawImage(b,new Rectangle(0,0,384,384),0,0,128,128,GraphicsUnit.Pixel);
                }clip.Add(canvas);delay.Add(group.Key=="rhythm"||group.Key=="lamp-overlay"?500:100);
            }
            Gif(Path.Combine(root,"qc","clips",group.Key+".gif"),clip,delay);foreach(Bitmap b in clip)b.Dispose();
        }
    }
    static void Overview(string root,Dictionary<string,Bitmap[]> groups,Bitmap identity){
        using(Bitmap board=new Bitmap(1536,1536))using(Graphics g=Graphics.FromImage(board))using(Font f=new Font("Arial",15)){
            g.Clear(Color.FromArgb(224,224,224));g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
            g.DrawString("SONIC B - APPROVED",f,Brushes.DarkSlateGray,14,10);g.DrawImage(identity,new Rectangle(0,32,384,384),0,0,128,128,GraphicsUnit.Pixel);
            int slot=1;
            foreach(KeyValuePair<string,Bitmap[]> group in groups){
                int i=group.Key=="rhythm"||group.Key=="lamp-overlay"?3:group.Key.Contains("attack")||group.Key.StartsWith("wave")?2:0;
                Bitmap b=group.Value[i];int x=slot%4*384,y=slot/4*448,s=b.Width==48?6:3;
                g.DrawString(group.Key+" ("+group.Value.Length+")",f,Brushes.DarkSlateGray,x+14,y+10);
                g.DrawImage(b,new Rectangle(x+(384-b.Width*s)/2,y+32,b.Width*s,b.Height*s),0,0,b.Width,b.Height,GraphicsUnit.Pixel);slot++;
            }board.Save(Path.Combine(root,"qc","overview.png"),ImageFormat.Png);
        }
    }
    static bool Same(Bitmap a,Bitmap b){
        if(a.Width!=b.Width||a.Height!=b.Height)return false;
        for(int y=0;y<a.Height;y++)for(int x=0;x<a.Width;x++)if(a.GetPixel(x,y).ToArgb()!=b.GetPixel(x,y).ToArgb())return false;return true;
    }
    static void Validate(string root,Dictionary<string,Bitmap[]> groups,Bitmap identity){
        List<string> report=new List<string>();report.Add("# Form B full production validation");report.Add("");int total=0;
        foreach(KeyValuePair<string,Bitmap[]> group in groups){
            int w=0,h=0;HashSet<int> colors=new HashSet<int>();
            bool body=group.Key=="idle"||group.Key=="attack"||group.Key=="attack-shock"||group.Key=="rhythm";
            foreach(Bitmap b in group.Value){
                Rectangle r=Bounds(b);w=Math.Max(w,r.Width);h=Math.Max(h,r.Height);
                for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++){
                    Color c=b.GetPixel(x,y);if(c.A!=0&&c.A!=255)throw new Exception("Partial alpha "+group.Key);
                    if(c.A>0){colors.Add(c.ToArgb());if(c.R+c.G+c.B==0)throw new Exception("Pure black "+group.Key);}
                    if(body&&y>=BaseY&&c.ToArgb()!=identity.GetPixel(x,y).ToArgb())throw new Exception("Moving base "+group.Key);
                }
                if(body&&r.Bottom-1!=96)throw new Exception("Body baseline");
                if(group.Key.StartsWith("wave")&&(r.Left<24||r.Right>112||r.Top<40||r.Bottom>88))throw new Exception("Wave cone extent");
            }
            total+=group.Value.Length;
            report.Add(String.Format("- {0}: {1} frames; canvas{2}x{3}; max bounds{4}x{5}; {6} colors.",group.Key,group.Value.Length,group.Value[0].Width,group.Value[0].Height,w,h,colors.Count));
        }
        foreach(Bitmap b in new[]{groups["idle"][0],groups["idle"][3],groups["attack"][0],groups["attack"][5],groups["attack-shock"][0],groups["attack-shock"][5],groups["rhythm"][0]})
            if(!Same(b,identity))throw new Exception("Rest pose differs from approved B");
        for(int i=1;i<4;i++){
            if(Same(groups["rhythm"][i-1],groups["rhythm"][i]))throw new Exception("Lamp state has no visual change");
            Bitmap expected=(Bitmap)identity.Clone();Blit(expected,groups["lamp-overlay"][i],0,0);if(!Same(expected,groups["rhythm"][i]))throw new Exception("Lamp overlay mismatch");expected.Dispose();
        }
        if(Same(groups["idle"][1],identity)&&Same(groups["idle"][2],identity))throw new Exception("Idle has no motion");
        report.Add("");report.Add(total+" individual frames excluding identity; 10 transparent group sheets.");
        report.Add("Binary alpha; no pure black. Fixed body base/neck y>=78 matches approved B exactly. Bottom y96.");
        report.Add("Idle1/4, attack1/6, attack-shock1/6 and rhythm0 exactly equal approved B.");
        report.Add("Lamps0..3 differ cumulatively; ON/OFF pixels sampled from existing image_gen B sources. Rhythm full bodies equal identity+lamp overlays.");
        report.Add("Membrane patches sampled from image_gen body sheets; fixed approved silhouette. Normal recoil1pxRIGHT, shock2/1pxRIGHT; overlay follows same shift.");
        report.Add("Normal/shock waves share max88x48 and right emission anchor(112,64); body mouth pair anchor(32,50). Logical per-frame common sequence scales preserve expansion/dissipation.");
        report.Add("49 decoded composite GIF frames: 2idlecycles, 3normalshots and1shock; one-second shot cadence; lamps0->1->2->3->0. Indexed palettes preserve pixel colors.");
        report.Add("Enemy RatRunner read-only reference in QC only. Preview push10 logical px (30px at3x), illustrating0.8cell for normal enemies; heavy proposal0.4cell. Scene is not game simulation; mechanics.md defines the proposal.");
        report.Add("No solid projectile, cooldown, form A, Unity import or gameplay changes. Raw sources and prompt set retained local.");
        File.WriteAllLines(Path.Combine(root,"qc","validation-production.md"),report,new UTF8Encoding(false));
    }
    public static void PreviewOnly(string root){
        string[] names={"idle","attack","attack-shock","lamp-overlay","wave-normal","wave-shock","hit-normal","hit-shock"};
        string[] prefixes={"idle","attack","attack-shock","lamps","wave","wave","hit","hit"};
        int[] counts={4,6,6,4,4,6,4,4};
        Dictionary<string,Bitmap[]> groups=new Dictionary<string,Bitmap[]>();
        try{
            for(int j=0;j<names.Length;j++){
                Bitmap[] frames=new Bitmap[counts[j]];groups.Add(names[j],frames);
                for(int i=0;i<frames.Length;i++)frames[i]=new Bitmap(Path.Combine(root,names[j],prefixes[j]+"-"+(i+1)+".png"));
            }
            Preview(root,groups,false);
        }finally{foreach(Bitmap[] frames in groups.Values)foreach(Bitmap b in frames)if(b!=null)b.Dispose();}
    }
    public static void Run(string root){
        using(Bitmap identity=new Bitmap(Path.Combine(root,"identity","identity-1.png")))
        using(Bitmap ready=new Bitmap(Path.Combine(root,"concept","rhythm-ready-b.png"))){
            BodyBounds=Bounds(identity);Dictionary<string,Bitmap[]> groups=new Dictionary<string,Bitmap[]>();
            groups.Add("idle",Bodies(root,"idle",identity));groups.Add("attack",Bodies(root,"attack",identity));groups.Add("attack-shock",Bodies(root,"attack-shock",identity));
            Bitmap[] lamps=Lamps(identity,ready);groups.Add("rhythm",Rhythms(identity,lamps));groups.Add("lamp-overlay",lamps);
            groups.Add("wave-normal",Effects(root,"wave-normal",4,88,48,true));groups.Add("wave-shock",Effects(root,"wave-shock",6,88,48,true));
            groups.Add("hit-normal",Effects(root,"hit-normal",4,16,14,false));groups.Add("hit-shock",Effects(root,"hit-shock",4,22,18,false));
            using(Bitmap raw=new Bitmap(Path.Combine(root,"raw","icon.png"))){
                Rectangle r=Bounds(raw);double scale=Math.Min(42.0/r.Width,40.0/r.Height);int w=(int)Math.Round(r.Width*scale),h=(int)Math.Round(r.Height*scale);
                Bitmap icon=new Bitmap(48,48);using(Bitmap fit=Sample(raw,r,w,h))Blit(icon,fit,(48-w)/2,(48-h)/2);groups.Add("icon",new[]{icon});
            }
            foreach(KeyValuePair<string,Bitmap[]> group in groups){
                if(group.Key=="icon"){Directory.CreateDirectory(Path.Combine(root,"icon"));group.Value[0].Save(Path.Combine(root,"icon","icon-cannon-sonic.png"),ImageFormat.Png);group.Value[0].Save(Path.Combine(root,"icon","sheet-transparent.png"),ImageFormat.Png);}
                else SaveGroup(root,group.Key,group.Key=="rhythm"?"rhythm":group.Key=="lamp-overlay"?"lamps":group.Key=="wave-normal"||group.Key=="wave-shock"?"wave":group.Key.StartsWith("hit")?"hit":group.Key,group.Value,group.Value.Length==6?3:2);
            }
            Board(root,groups);Overview(root,groups,identity);Preview(root,groups);Validate(root,groups,identity);
            foreach(Bitmap[] group in groups.Values)foreach(Bitmap b in group)b.Dispose();
        }
    }
}

'@
Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing
if($PreviewOnly){
    [SonicExport]::PreviewOnly($root)
    'Composite preview rebuilt and all49 frames decoded; maximum push10 logical px.'
}else{
    [SonicExport]::Run($root)
    Get-Content -Encoding utf8 (Join-Path $root 'qc/validation-production.md')
}
