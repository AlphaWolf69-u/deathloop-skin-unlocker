using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
[assembly:System.Reflection.AssemblyTitle("AlphaWolf's Deathloop Skin Unlocker")]
[assembly:System.Reflection.AssemblyProduct("Deathloop Skin Unlocker")]
[assembly:System.Reflection.AssemblyCompany("AlphaWolf")]
[assembly:System.Reflection.AssemblyVersion("1.0.2.0")]
namespace AlphaWolfUnlocker {
sealed class Mem : IDisposable {
 [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint a,bool b,int c);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr a,byte[] b,UIntPtr n,out UIntPtr done);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h,IntPtr a,byte[] b,UIntPtr n,out UIntPtr done);
 [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
 [DllImport("kernel32.dll")] static extern UIntPtr VirtualQueryEx(IntPtr h,IntPtr a,out Region r,UIntPtr n);
 [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr VirtualAllocEx(IntPtr h,IntPtr a,UIntPtr n,uint type,uint protection);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool VirtualProtectEx(IntPtr h,IntPtr a,UIntPtr n,uint p,out uint old);
 [DllImport("kernel32.dll")] static extern bool FlushInstructionCache(IntPtr h,IntPtr a,UIntPtr n);
 [StructLayout(LayoutKind.Sequential)] struct Region { public long Address,Allocation;public uint AllocationProtection;public ushort Partition;public long Size;public uint State,Protection,Type; }
 public Process Process;public long Base;IntPtr handle;
 public Mem(Process p){Process=p;Base=p.MainModule.BaseAddress.ToInt64();handle=OpenProcess(0x438,false,p.Id);if(handle==IntPtr.Zero)throw new Exception("Access denied. Run this app as administrator.");}
 public byte[] Read(long a,int n){if(a<65536||n<0||n>0x2000000)throw new Exception("Waiting for game data");byte[] b=new byte[n];UIntPtr done;if(!ReadProcessMemory(handle,new IntPtr(a),b,(UIntPtr)n,out done)||done.ToUInt64()!=(ulong)n)throw new Exception("Waiting for game data");return b;}
 public long Q(long a){return BitConverter.ToInt64(Read(a,8),0);}public uint U(long a){return BitConverter.ToUInt32(Read(a,4),0);}public ushort W(long a){return BitConverter.ToUInt16(Read(a,2),0);}
 public string Text(long a){byte[] b=Read(a,256);int n=Array.IndexOf(b,(byte)0);return Encoding.UTF8.GetString(b,0,n<0?b.Length:n);}
 public void Write(long a,byte[] b){UIntPtr done;if(!WriteProcessMemory(handle,new IntPtr(a),b,(UIntPtr)b.Length,out done)||done.ToUInt64()!=(ulong)b.Length)throw new Exception("Could not update game data");}
 public void Set(long a,uint value){Write(a,BitConverter.GetBytes(value));if(U(a)!=value)throw new Exception("Game data changed during update");}
 public long Allocate(){long p=VirtualAllocEx(handle,IntPtr.Zero,(UIntPtr)8192,0x3000,4).ToInt64();if(p==0)throw new Exception("Could not allocate callback");return p;}
 public uint Protect(long a,int n,uint p){uint old;if(!VirtualProtectEx(handle,new IntPtr(a),(UIntPtr)n,p,out old))throw new Exception("Could not prepare menu refresh");return old;}
 public void Flush(long a,int n){FlushInstructionCache(handle,new IntPtr(a),(UIntPtr)n);}
 public long ScannedBytes;
 public IEnumerable<long> Find(long value,CancellationToken token,long start,long end){
  // Writable committed heap pages only; no debugger or game execution during discovery.
  byte first=(byte)value;
  for(long at=Math.Max(65536,start);at<end;){token.ThrowIfCancellationRequested();Region r;if(VirtualQueryEx(handle,new IntPtr(at),out r,(UIntPtr)Marshal.SizeOf(typeof(Region)))==UIntPtr.Zero)yield break;
   long next=r.Address+r.Size;if(next<=at)yield break;
   uint p=r.Protection&255;
   if(r.State==0x1000&&(r.Protection&0x100)==0&&(p==4||p==8||p==0x40||p==0x80)){
    for(long a=at;a<Math.Min(next,end);a+=0x100000){token.ThrowIfCancellationRequested();byte[] data=null;try{data=Read(a,(int)Math.Min(0x100000,Math.Min(next,end)-a));}catch{}if(data==null)continue;
     ScannedBytes+=data.Length;
     for(int i=0;i+8<=data.Length;i+=8)if(data[i]==first&&BitConverter.ToInt64(data,i)==value)yield return a+i;
    }
   }at=next;
  }
 }
 public void Dispose(){if(handle!=IntPtr.Zero){CloseHandle(handle);handle=IntPtr.Zero;}Process.Dispose();}
}
sealed class Item {public uint Id,Flags;public long Record,Definition;public string Path;}
sealed class Engine : IDisposable {
 public string Status="Waiting for Deathloop";public Mem M;long menu,root,model;string stable="";DateTime stableAt;bool refreshPending;
 readonly Dictionary<int,string> choices=new Dictionary<int,string>();
 public long MenuHint;public bool Searching;
 public string DisplayStatus {get{return Searching&&M!=null?"Finding outfit menu — "+(M.ScannedBytes/1048576)+" MB checked":Status;}}
 public bool ValidMenu(long p){try{return p>65536&&M.Q(p)==M.Base+0x26880F0&&M.Q(p+0x2E0)>65536&&M.U(p+0x334)<=64&&M.U(p+0x344)<=64;}catch{return false;}}
 Dictionary<int,List<Item>> Inventory(){var result=new Dictionary<int,List<Item>>();foreach(int c in new[]{1,3}){long a=root+0x590+c*0xA0;if(M.U(a+0x60)!=c)throw new Exception("Waiting for player inventory");uint n=M.U(a+0x1C);if(n>10000)throw new Exception("Waiting for player inventory");long arr=M.Q(a+0x10);var list=new List<Item>();for(int i=0;i<n;i++){long r=arr+i*0x58;if(M.U(r+0x10)!=4)continue;long d=M.Q(r+8);string name=M.Text(M.Q(d+8));if(!name.StartsWith("models/equipment/outfits/player_outfit_")||!name.EndsWith(".outfitinventoryitem"))throw new Exception("Outfit layout not recognized");list.Add(new Item{Id=M.U(r),Record=r,Definition=d,Flags=M.U(r+0x18),Path=name});}if(list.Count==0||list.Count>64)throw new Exception("Waiting for outfits");result[c]=list;}return result;}
 long Requirement(long manager,ushort id){uint mask=M.U(manager+0x30);if(mask==0||mask>65535)throw new Exception("Waiting for outfit requirements");long p=M.Q(M.Q(manager+0x20)+(id&mask)*8);for(int i=0;i<100&&p!=0;i++){ushort key=M.W(p);if(key==id)return p+4;if(key>id)return 0;p=M.Q(p+8);}return 0;}
 HashSet<uint> MenuIds(int c){long h=menu+(c==1?0x328:0x338);uint n=M.U(h+12);if(n>64)throw new Exception("Waiting for menu data");long a=M.Q(h);var ids=new HashSet<uint>();for(int i=0;i<n;i++)ids.Add(M.U(a+i*4));return ids;}
 public void Step(CancellationToken token){
  if(M==null||M.Process.HasExited){if(M!=null)M.Dispose();M=null;menu=0;stable="";var ps=Process.GetProcessesByName("Deathloop");if(ps.Length==0){Status="Waiting for Deathloop";return;}if(ps.Length!=1){foreach(var p in ps)p.Dispose();throw new Exception("More than one Deathloop process is running");}M=new Mem(ps[0]);}
  long game=M.Q(M.Base+0x5BD1010);
  // A null game pointer is NORMAL during loading. Never count it as failure or exit.
  if(game==0){stable="";Status="Loading — waiting for the game";return;}
  long info=M.Q(game+0x3568);if(info==0){stable="";Status="Loading — waiting for the game";return;}
  string map=M.Text(M.Q(info+0x98));if(map!="maps/campaign/menu/menu.map"){stable="";Status="Active — outfits will refresh on return to loadout";return;}
  root=M.Q(M.Q(M.Base+0x333A150));if(root==0){stable="";Status="Waiting for player profile";return;}
  if(!ValidMenu(menu)){
   if(ValidMenu(MenuHint))menu=MenuHint;
   else {Status="Finding outfit menu";Searching=true;M.ScannedBytes=0;try{
    // The live menu is commonly allocated near the current game object.
    // Search there first, then retain the full heap search for other layouts.
    long nearStart=Math.Max(65536,(game&~65535L)-0x2000000),nearEnd=Math.Min(M.Base,(game&~65535L)+0x4000000);
    foreach(long p in M.Find(M.Base+0x26880F0,token,nearStart,nearEnd)){if(ValidMenu(p)){menu=p;break;}}
    if(!ValidMenu(menu))foreach(long p in M.Find(M.Base+0x26880F0,token,65536,M.Base)){if(ValidMenu(p)){menu=p;break;}}
   }finally{Searching=false;}}
   if(!ValidMenu(menu)){Status="Waiting for loadout menu";return;}stable="";
  }
  model=M.Q(menu+0x2E0);string sig=root+":"+game+":"+model;
  if(sig!=stable){stable=sig;stableAt=DateTime.UtcNow;refreshPending=true;Status="Preparing outfit menu";return;}
  if((DateTime.UtcNow-stableAt).TotalSeconds<1.2)return;
  var inventories=Inventory();long manager=M.Q(game+0x34E8);if(manager==0){Status="Waiting for outfits";return;}
  var patches=new Dictionary<long,uint>();bool rebuild=false;
  foreach(var entry in inventories){int c=entry.Key;var items=entry.Value;long a=root+0x590+c*0xA0;uint selected=M.U(a+0x58);bool reset=items.Any(x=>(x.Flags&8)!=0);var selectedItem=items.FirstOrDefault(x=>x.Id==selected);var ids=MenuIds(c);
   foreach(var item in items){long req=Requirement(manager,M.W(item.Definition+0x2A0));if(req!=0&&M.U(req)!=0)patches[req]=0;if((item.Flags&8)!=0)patches[item.Record+0x18]=(item.Flags&~8u)|2u;if(!ids.Contains(item.Id))rebuild=true;}
   string remembered;string defaultPath="models/equipment/outfits/player_outfit_"+(c==1?"berezin_a01":"julianna_a01")+"_item.outfitinventoryitem";
   // Restore only a transition fallback, not a deliberate selection in an already-unlocked menu.
   if(reset&&selectedItem!=null&&selectedItem.Path==defaultPath&&choices.TryGetValue(c,out remembered)){
    Item desired=items.FirstOrDefault(x=>x.Path==remembered);if(desired!=null&&desired.Id!=selected){patches[a+0x58]=desired.Id;rebuild=true;}
   }else if(!reset&&selectedItem!=null){choices[c]=selectedItem.Path;}
  }
  if(patches.Count!=0||rebuild)refreshPending=true;
  if(refreshPending){if(M.Q(M.Base+0x5BD1010)!=game||M.Q(M.Q(M.Base+0x333A150))!=root||M.Q(menu+0x2E0)!=model)throw new Exception("Loading — waiting for stable menu");
   var before=patches.ToDictionary(p=>p.Key,p=>M.U(p.Key));var written=new List<long>();try{foreach(var p in patches){if(M.U(p.Key)!=before[p.Key])throw new Exception("Loading — outfit data changed");written.Add(p.Key);M.Set(p.Key,p.Value);}}catch{foreach(long p in written.AsEnumerable().Reverse())try{if(M.U(p)==patches[p])M.Set(p,before[p]);}catch{}throw;}
   Refresh();foreach(var pair in inventories)if(!MenuIds(pair.Key).IsSupersetOf(pair.Value.Select(x=>x.Id)))throw new Exception("Menu refresh incomplete; retrying");refreshPending=false;
  }
  Status="Ready: Colt "+inventories[1].Count+", Julianna "+inventories[3].Count+" — reopen outfit screen if unchanged";
 }
 void Refresh(){
  long b=M.Base,iat=b+0x231DB48,original=M.Q(iat);if(original!=M.Q(b+0x20DC4628))throw new Exception("Another tool is using the menu callback");
  if(!M.Read(b+0xBEFD2D,6).SequenceEqual(new byte[]{0xff,0x15,0x15,0xde,0x72,0x01}))throw new Exception("Menu callback layout not recognized");
  byte[] prologue={0x40,0x55,0x56,0x57,0x41,0x54,0x41,0x55,0x41,0x56,0x41,0x57,0x48,0x8d};if(!M.Read(b+0x1086680,prologue.Length).SequenceEqual(prologue))throw new Exception("Menu function layout not recognized");
  long block=M.Allocate(),state=block+4096;var code=new List<byte>();var branches=new List<int>();Action<string> emit=s=>code.AddRange(s.Split(' ').Where(x=>x.Length!=0).Select(x=>Convert.ToByte(x,16)));Action<long> imm=x=>code.AddRange(BitConverter.GetBytes(x));Action jne=()=>{emit("0f 85");branches.Add(code.Count);code.AddRange(new byte[4]);};
  emit("51 52 41 50 41 51 53 48 83 ec 20 48 bb");imm(state);
  emit("48 8b 44 24 48 49 ba");imm(b+0xBEFD33);emit("4c 39 d0");jne();
  emit("48 b8");imm(menu);emit("49 ba");imm(b+0x26880F0);emit("4c 39 10");jne();emit("49 ba");imm(model);emit("4c 39 90 e0 02 00 00");jne();
  emit("48 b8");imm(b+0x333A150);emit("48 8b 00 48 8b 00 49 ba");imm(root);emit("4c 39 d0");jne();
  emit("31 c0 41 ba 01 00 00 00 f0 44 0f b1 13");jne();emit("49 ba");imm(iat);emit("48 b8");imm(original);emit("49 89 02");
  foreach(int c in new[]{1,3}){emit("48 b9");imm(menu);emit("ba");code.AddRange(BitConverter.GetBytes(c));emit("48 b8");imm(b+0x1086680);emit("ff d0");}
  emit("c7 03 02 00 00 00");int end=code.Count;emit("48 83 c4 20 5b 41 59 41 58 5a 59 ff 25 00 00 00 00");imm(original);foreach(int at in branches){byte[] bytes=BitConverter.GetBytes(end-at-4);for(int i=0;i<4;i++)code[at+i]=bytes[i];}
  M.Write(block,code.ToArray());M.Protect(block,4096,0x20);M.Flush(block,code.Count);uint protection=M.Protect(iat,8,4);
  try{if(M.Q(iat)!=original)throw new Exception("Menu callback changed");M.Write(iat,BitConverter.GetBytes(block));var deadline=DateTime.UtcNow.AddSeconds(10);while(M.U(state)!=2&&DateTime.UtcNow<deadline)Thread.Sleep(40);if(M.U(state)!=2)throw new Exception("Waiting for game thread to refresh menu");}
  finally{if(M.Q(iat)==block)M.Write(iat,BitConverter.GetBytes(original));M.Protect(iat,8,protection);}
  // Keep 8 KiB allocated until process exit: another message-pump call may still be returning.
 }
 public void Error(Exception e){stable="";Status=e.Message;}
 public void Dispose(){if(M!=null){M.Dispose();M=null;}}
}
sealed class Tray : ApplicationContext {
 readonly Engine engine=new Engine();readonly NotifyIcon icon=new NotifyIcon();readonly ContextMenuStrip menu=new ContextMenuStrip();readonly ToolStripMenuItem status=new ToolStripMenuItem("Waiting for Deathloop");readonly ToolStripMenuItem enabled=new ToolStripMenuItem("Keep outfits unlocked");readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();readonly CancellationTokenSource cancel=new CancellationTokenSource();bool busy,closing,finished;
 public Tray(long menuHint,bool smoke=false){engine.MenuHint=menuHint;status.Enabled=false;enabled.Checked=true;enabled.CheckOnClick=true;
  menu.Items.Add(status);menu.Items.Add(new ToolStripSeparator());menu.Items.Add(enabled);menu.Items.Add("Exit",null,(s,e)=>Stop());
  icon.Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);icon.Text="AlphaWolf's Deathloop Skin Unlocker";icon.ContextMenuStrip=menu;icon.Visible=true;
  timer.Interval=500;timer.Tick+=async(s,e)=>await Tick();if(!smoke)timer.Start();
 }
 async Task Tick(){if(busy||closing)return;if(!enabled.Checked){status.Text="Paused";return;}busy=true;try{var work=Task.Run(()=>{try{engine.Step(cancel.Token);}catch(OperationCanceledException){}catch(Exception e){engine.Error(e);}});while(!work.IsCompleted){status.Text=engine.DisplayStatus;await Task.Delay(200);}await work;status.Text=engine.DisplayStatus;}finally{busy=false;if(closing)Finish();}}
 void Stop(){closing=true;timer.Stop();cancel.Cancel();if(!busy)Finish();}
 void Finish(){if(finished)return;finished=true;icon.Visible=false;engine.Dispose();ExitThread();}
 protected override void Dispose(bool disposing){if(disposing){timer.Stop();cancel.Cancel();icon.Visible=false;icon.Dispose();timer.Dispose();menu.Dispose();if(!busy)engine.Dispose();cancel.Dispose();}base.Dispose(disposing);}
}
static class Program {
 [STAThread] static int Main(string[] args){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);if(args.Length==1&&args[0]=="--ui-smoke"){using(var t=new Tray(0,true)){}return 0;}bool owned;using(var mutex=new Mutex(true,"Local\\AlphaWolfStandaloneUnlocker",out owned)){if(!owned){MessageBox.Show("Deathloop Skin Unlocker is already running.","AlphaWolf's Deathloop Skin Unlocker");return 1;}long hint=0;if(args.Length==2&&args[0]=="--menu")hint=Convert.ToInt64(args[1],16);using(var tray=new Tray(hint))Application.Run(tray);return 0;}}
}
}
