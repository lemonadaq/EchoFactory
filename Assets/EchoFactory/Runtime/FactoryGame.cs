using System;
using System.Linq;
using EchoFactory.Core;
using UnityEngine;
namespace EchoFactory.Runtime
{
    // Prototype presentation only; no production or movement rules belong here.
    public sealed class FactoryGame : MonoBehaviour
    {
        private GameSession game;
        private bool paused, help, abort, errorsOnly;
        private double accumulator;
        private float speed = 1;
        private int selected = 2, tab, placement = -1, quantity = 1;
        private StationKind newKind;
        private Material newMaterial;
        private string echoName = "", saveError = "", notice = "", fatal = "";
        private Vector2 scroll;
        private GUIStyle title, heading, body, small, button, field;
        private static readonly Color Bg = new Color(.055f,.075f,.09f), Panel = new Color(.10f,.135f,.16f), Cyan = new Color(.43f,.85f,.8f), Amber = new Color(.97f,.77f,.42f), Muted = new Color(.64f,.72f,.76f);
        private const float CellSize=58, GridX=24, GridY=130, ViewWidth=1440, ViewHeight=930;
        private bool Planning { get { return game.Phase == GamePhase.Planning; } }
        private void Awake()
        {
            Application.targetFrameRate=60;
            if(Camera.main==null) { var go=new GameObject("Factory Camera"); var c=go.AddComponent<Camera>(); go.tag="MainCamera"; c.orthographic=true; c.clearFlags=CameraClearFlags.SolidColor; c.backgroundColor=Bg; go.transform.position=new Vector3(0,0,-10); }
            Load();
        }
        private void Load()
        {
            try { game=new GameSession(LocalSave.Load()); fatal=""; echoName="Echo "+game.Data.NextEchoId; }
            catch(Exception e) { fatal=e.Message; game=null; }
        }
        private void Save()
        {
            if(game==null)return;
            try { LocalSave.Save(game.Data); saveError=""; } catch(Exception e) { saveError="Błąd zapisu: "+e.Message; }
        }
        private void Change(bool success) { if(success)Save(); notice=game.Message; }
        private void Update()
        {
            if(game==null||paused||help||abort)return;
            if(game.Phase!=GamePhase.Recording&&game.Phase!=GamePhase.Autoloop) { accumulator=0;return; }
            accumulator+=Math.Min(Time.unscaledDeltaTime,.25f)*speed; int oldShift=game.Data.Shift;
            while(accumulator>=1.0/Rules.TickRate) { accumulator-=1.0/Rules.TickRate; game.Tick(); if(game.Phase==GamePhase.Summary){accumulator=0;break;} }
            if(oldShift!=game.Data.Shift) { Save();echoName="Echo "+game.Data.NextEchoId; }
        }
        private void OnApplicationFocus(bool focused) { if(!focused)paused=true; }
        private void OnApplicationQuit() { Save(); }
        private void InitStyles()
        {
            if(body!=null)return;
            title=new GUIStyle(GUI.skin.label){fontSize=31,fontStyle=FontStyle.Bold};
            heading=new GUIStyle(GUI.skin.label){fontSize=21,fontStyle=FontStyle.Bold,wordWrap=true};
            body=new GUIStyle(GUI.skin.label){fontSize=16,wordWrap=true}; small=new GUIStyle(body){fontSize=13};
            button=new GUIStyle(GUI.skin.button){fontSize=15,wordWrap=true,padding=new RectOffset(8,8,5,5)};
            field=new GUIStyle(GUI.skin.textField){fontSize=17,padding=new RectOffset(10,10,8,8)};
            foreach(var s in new[]{title,heading,body,small})s.normal.textColor=Color.white;
        }
        private void Fill(Rect r,Color color) { var old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old; }
        private void Label(float x,float y,float w,float h,string value,GUIStyle style=null,Color? color=null)
        { var old=GUI.color;GUI.color=color??Color.white;GUI.Label(new Rect(x,y,w,h),value,style??body);GUI.color=old; }
        private bool Btn(float x,float y,float w,float h,string value,bool enabled=true,bool primary=false)
        {
            bool old=GUI.enabled;var oldColor=GUI.backgroundColor;GUI.enabled=old&&enabled;GUI.backgroundColor=primary?Cyan:new Color(.30f,.40f,.44f);
            bool hit=GUI.Button(new Rect(x,y,w,h),value,button);GUI.enabled=old;GUI.backgroundColor=oldColor;return hit;
        }
        private void OnGUI()
        {
            InitStyles();GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/ViewWidth,Screen.height/ViewHeight,1));Fill(new Rect(0,0,ViewWidth,ViewHeight),Bg);
            Label(24,22,650,46,"ECHO FACTORY",title,Cyan);Label(25,69,760,26,"HALA 01 / PROTOTYP UNITY / LINIA PRODUKCYJNA",small,Muted);
            if(game==null)
            {
                Label(50,150,1250,160,"Nie można wczytać zapisu. "+fatal,heading);
                if(Btn(50,350,340,50,"Przywróć kopię zapasową")) { try{LocalSave.RestoreBackup();Load();}catch(Exception e){fatal=e.Message;} }
                Label(50,430,1250,100,"Zapis pozostawiono bez zmian: "+LocalSave.PathName);return;
            }
            Label(950,24,450,36,game.Data.Credits+" CREDITS · ZMIANA "+game.Data.Shift,heading);
            string phase=Planning?"PLANOWANIE":game.Phase==GamePhase.Summary?"PODSUMOWANIE":game.Phase==GamePhase.Autoloop?"AUTOLOOP":"NAGRYWANIE";
            Label(950,65,450,28,phase+(paused?" / PAUZA":""),body,Cyan);
            bool modal=help||abort||game.Phase==GamePhase.Summary;GUI.enabled=!modal;
            Grid();Controls();SidePanel();
            Label(24,858,1390,50,saveError.Length>0?saveError:notice.Length>0?notice:game.Message,body,saveError.Length>0?Amber:Cyan);
            Label(24,910,1390,20,"WASD / strzałki: ruch · E: akcja · 1/2/3: podajnik/prasa/wysyłka · Spacja: pauza · Tab: panel",small,Muted);
            GUI.enabled=true;
            if(help)Help();else if(abort)AbortDialog();else if(game.Phase==GamePhase.Summary)Summary();
            if(!modal)Keys();
        }
        private void Grid()
        {
            var sim=game.Simulation;Label(GridX,101,800,25,placement>=0?"WSKAŻ NOWE POŁOŻENIE · ESC ANULUJE":"WYBIERZ STACJĘ · AKCJE W PANELU PO PRAWEJ",small,Muted);
            string placementHint = "";
            var hover = new Cell(Mathf.FloorToInt((Event.current.mousePosition.x-GridX)/CellSize), Mathf.FloorToInt((Event.current.mousePosition.y-GridY)/CellSize));
            bool previewValid = false; var affected = new System.Collections.Generic.List<int>();
            if(placement>=0 && hover.X>=0 && hover.X<Rules.Width && hover.Y>=0 && hover.Y<Rules.Height)
                previewValid=game.PreviewPlacement(placement,hover,out placementHint,out affected,newKind,newMaterial);
            for(int y=0;y<Rules.Height;y++)for(int x=0;x<Rules.Width;x++)
            {
                Rect r=new Rect(GridX+x*CellSize,GridY+y*CellSize,CellSize-2,CellSize-2);var cell=new Cell(x,y);
                Fill(r,(x+y)%2==0?new Color(.13f,.18f,.21f):new Color(.115f,.16f,.19f));if(cell==Rules.Spawn)Label(r.x+4,r.y+35,50,18,"START",small,Muted);
                if(placement>=0 && cell==hover)Fill(r,!previewValid?new Color(.65f,.22f,.22f):affected.Count>0?new Color(.65f,.48f,.18f):new Color(.22f,.48f,.36f));
                if(GUI.enabled&&Event.current.type==EventType.MouseDown&&Event.current.button==0&&r.Contains(Event.current.mousePosition))
                {
                    if(placement>=0) { bool ok=game.Place(placement,cell,newKind,newMaterial);Change(ok);if(ok){selected=placement==0?game.Data.Layout.NextStationId-1:placement;placement=-1;} }
                    else { var st=sim.Stations.Find(s=>s.Spec.Position==cell||s.Spec.Port==cell);if(st!=null){selected=st.Spec.Id;tab=0;}else if(game.Phase==GamePhase.Recording&&!paused){string why;sim.EnqueueMove(cell,out why);notice=why;} }
                    Event.current.Use();
                }
            }
            foreach(var s in sim.Stations)
            {
                float x=GridX+s.Spec.Position.X*CellSize,y=GridY+s.Spec.Position.Y*CellSize;
                Color color=s.Spec.Kind==StationKind.Feeder?Amber:s.Spec.Kind==StationKind.Dispatch?new Color(.6f,.85f,.62f):Cyan;
                Fill(new Rect(x+3,y+3,CellSize-8,CellSize-8),selected==s.Spec.Id?color:new Color(.27f,.39f,.42f));
                Label(x+7,y+7,49,25,s.Spec.Kind==StationKind.Feeder?"RUDA":s.Spec.Kind==StationKind.Press?"PRASA":s.Spec.Kind==StationKind.Dispatch?"WYŚL.":"BUFOR",small,Bg);
                Label(x+7,y+30,48,22,"#"+s.Spec.Id,small,Bg);Fill(new Rect(x+8,y+CellSize+4,CellSize-18,5),color);
                if(s.ProcessEnd>0)Fill(new Rect(x+4,y+CellSize-9,(CellSize-10)*(1f-(s.ProcessEnd-sim.Tick)/(float)s.Spec.ProcessTicks),4),Color.white);
            }
            foreach(var u in sim.Units)
            {
                float x=u.Position.X,y=u.Position.Y;var c=u.Current;
                if(u.Moving&&c!=null&&u.PathIndex<c.Path.Count){float a=Mathf.Clamp01(1f-(u.EndTick-sim.Tick)/(float)u.Profile.MoveTicks);x=Mathf.Lerp(x,c.Path[u.PathIndex].X,a);y=Mathf.Lerp(y,c.Path[u.PathIndex].Y,a);}
                int offset=sim.Units.Where(v=>v.Position==u.Position).TakeWhile(v=>v.Id!=u.Id).Count();Rect r=new Rect(GridX+x*CellSize+8+offset*6,GridY+y*CellSize+15+offset*4,35,29);
                Fill(r,u.IsOperator?Amber:Cyan);Label(r.x+2,r.y+4,38,22,u.IsOperator?"TY":"E"+u.Id,small,Bg);
                if(u.CargoCount>0)Label(r.x-2,r.y+29,57,22,u.CargoCount+(u.Cargo==Material.Ore?" R":" P"),small,u.IsOperator?Amber:Cyan);
            }
            var problem = tab==3 ? sim.FirstProblem() : null;
            if(problem!=null)
            {
                var cell=problem.Position; float px=GridX+cell.X*CellSize, py=GridY+cell.Y*CellSize;
                if(cell.X>=0&&cell.X<Rules.Width&&cell.Y>=0&&cell.Y<Rules.Height)
                {
                    var red=new Color(1f,.35f,.3f);
                    Fill(new Rect(px,py,CellSize-2,3),red);Fill(new Rect(px,py+CellSize-5,CellSize-2,3),red);
                    Fill(new Rect(px,py,3,CellSize-2),red);Fill(new Rect(px+CellSize-5,py,3,CellSize-2),red);
                }
            }
            Label(24,663,812,38,placementHint.Length>0?placementHint:"R = ruda · P = płyta · żółty: Operator · cyjan: Echo · pasek pod stacją: punkt obsługi",small,placementHint.Length>0?Amber:Muted);
        }
        private void Controls()
        {
            var sim=game.Simulation;Label(24,703,200,38,(sim.Tick/20).ToString("00")+" / 60 s",heading);
            Fill(new Rect(213,714,402,6),Panel);Fill(new Rect(213,714,402f*sim.Tick/Rules.ShiftTicks,6),Cyan);Label(650,703,195,38,sim.Shipped+" PŁYT",heading,Cyan);
            if(Btn(24,751,300,47,game.ReplacingId==0?"Rozpocznij zmianę":"Nagraj zastępstwo",Planning,true)){Save();game.Start();paused=false;notice="";placement=-1;}
            if(Btn(335,751,160,47,paused?"Wznów":"Pauza",!Planning))paused=!paused;
            if(Btn(506,751,160,47,"Przerwij",!Planning))abort=true;
            if(Btn(677,751,160,47,speed==1?"Tempo 1×":"Tempo 0,5×"))speed=speed==1?.5f:1f;
            if(Btn(24,809,190,35,"Jak grać"))help=true;
            if(Btn(225,809,190,35,"Cofnij zakup / układ",Planning))Change(game.Undo());
            if(Btn(426,809,200,35,"Zapisz postęp",Planning))Save();
            if(Btn(637,809,200,35,"Anuluj ustawianie",placement>=0))placement=-1;
        }
        private void SidePanel()
        {
            Fill(new Rect(866,112,550,730),Panel);string[] tabs={"Stacja","Echo","Rozwój","Dziennik"};
            for(int i=0;i<4;i++)if(Btn(880+i*130,126,123,38,tabs[i],true,tab==i)){tab=i;scroll=Vector2.zero;}
            GUI.BeginGroup(new Rect(884,180,514,648));if(tab==0)StationPanel();else if(tab==1)EchoPanel();else if(tab==2)Upgrades();else Log();GUI.EndGroup();
        }
        private void StationPanel()
        {
            var s=game.Simulation.Station(selected);if(s==null){Label(0,0,500,80,"Wybierz stację na hali.");return;}
            Label(0,0,500,40,s.Spec.Name+" #"+s.Spec.Id,heading,Cyan);
            string info=s.Spec.Kind==StationKind.Press?"WEJŚCIE: "+s.Input+" / "+s.Spec.InputCapacity+" rudy\nWYJŚCIE: "+s.Output+" / "+s.Spec.OutputCapacity+" płyt\nCYKL: "+(s.Spec.ProcessTicks/20f).ToString("0.0")+" s":s.Spec.Kind==StationKind.Dispatch?"Wysyłka rozlicza płyty po pełnej zmianie.\nCena: "+Rules.PlatePrice+" Credits / płyta":"MAGAZYN: "+s.Output+" / "+s.Spec.OutputCapacity;
            Label(0,47,500,98,info);string status=s.ProcessEnd>0?"Produkcja: "+((s.ProcessEnd-game.Simulation.Tick)/20f).ToString("0.0")+" s":s.Spec.Kind==StationKind.Press?(s.Output>=s.Spec.OutputCapacity?"Pełny magazyn wyjściowy":s.Input==0?"Czeka na rudę":"Gotowość"):"Gotowość";
            Label(0,149,500,40,status,body,Amber);var op=game.Simulation.Operator;int cargo=op!=null?op.Profile.Cargo:game.Data.Layout.Operator.Cargo;quantity=Mathf.Clamp(quantity,1,cargo);
            Label(0,199,210,36,"Ilość na czynność: "+quantity);if(Btn(225,196,45,32,"−"))quantity=Math.Max(1,quantity-1);if(Btn(280,196,45,32,"+"))quantity=Math.Min(cargo,quantity+1);
            bool can=game.Phase==GamePhase.Recording&&!paused&&op!=null&&op.Idle;
            if(s.Spec.Kind!=StationKind.Dispatch){Material m=s.Spec.Kind==StationKind.Feeder?Material.Ore:s.Spec.Kind==StationKind.Buffer?s.Spec.BufferMaterial:Material.Plate;if(Btn(0,242,244,46,"Odbierz "+(m==Material.Ore?"rudę":"płyty"),can,true))Act(CommandKind.Pickup,m);}
            if(s.Spec.Kind!=StationKind.Feeder){Material m=s.Spec.Kind==StationKind.Press?Material.Ore:s.Spec.Kind==StationKind.Buffer?s.Spec.BufferMaterial:Material.Plate;if(Btn(255,242,244,46,s.Spec.Kind==StationKind.Dispatch?"Wyślij płyty":"Odłóż "+(m==Material.Ore?"rudę":"płyty"),can))Act(CommandKind.Deposit,m);}
            Label(0,305,500,85,op==null?"Test bez Operatora.":"Operator: "+op.CargoCount+" / "+op.Profile.Cargo+" · "+(op.Cargo==Material.None?"puste ręce":op.Cargo==Material.Ore?"ruda":"płyty")+"\n"+op.Status,body,Muted);
            if(Btn(0,403,499,42,"Przenieś budynek",Planning)){placement=selected;notice="Zależne nagrania: "+game.Dependents(selected)+". Kliknij nowe pole. Stare trasy wymagają sprawdzenia.";}
            var queue=game.Simulation.Units.Where(u=>u.Current!=null&&u.Current.TargetId==selected&&u.WaitSince>=0)
                .OrderBy(u=>u.Servicing?0:1).ThenBy(u=>u.WaitSince).ThenBy(u=>u.Id).ToList();
            string service=queue.Count==0?"Obsługa wolna. Brak oczekujących.":string.Join("\n",queue.Take(3).Select(u=>
                (u.IsOperator?"Operator":"Echo #"+u.Id)+": "+u.Status));
            if(queue.Count>3)service+="\nPozostałe oczekujące: "+(queue.Count-3);
            Label(0,455,500,103,service,small,Muted);
            Label(0,565,500,64,"Budowa i ulepszenia są w zakładce Rozwój. Zmiany wykonujesz między zmianami.",small,Muted);
        }
        private void Act(CommandKind kind,Material material){string why;game.Simulation.EnqueueAction(selected,kind,material,quantity,out why);notice=why;}
        private void Quick(int id)
        {
            if(game.Phase!=GamePhase.Recording||paused)return;var s=game.Simulation.Station(id);var u=game.Simulation.Operator;if(s==null||u==null)return;selected=id;
            if(s.Spec.Kind==StationKind.Feeder)Act(CommandKind.Pickup,Material.Ore);else if(s.Spec.Kind==StationKind.Dispatch)Act(CommandKind.Deposit,Material.Plate);
            else if(s.Spec.Kind==StationKind.Press)Act(u.Cargo==Material.Ore?CommandKind.Deposit:CommandKind.Pickup,u.Cargo==Material.Ore?Material.Ore:Material.Plate);
            else Act(u.Cargo==s.Spec.BufferMaterial?CommandKind.Deposit:CommandKind.Pickup,s.Spec.BufferMaterial);
        }
        private void EchoPanel()
        {
            Label(0,0,500,40,"Echo: "+game.Data.Echoes.Count(e=>e.Enabled)+" / 5",heading,Cyan);Label(0,42,500,63,"Zapisane nagrania pracują w kolejnych zmianach. Zastępstwo nagrywasz bez starej wersji Echo.",body,Muted);
            scroll=GUI.BeginScrollView(new Rect(0,112,510,327),scroll,new Rect(0,0,487,Math.Max(300,game.Data.Echoes.Count*112)));
            for(int i=0;i<game.Data.Echoes.Count;i++)
            {
                var e=game.Data.Echoes[i];float y=i*112;Label(0,y,465,25,"#"+e.Id+" "+e.Name+(e.Enabled?" · aktywne":" · wyłączone"),body,e.Enabled?Cyan:Muted);
                Label(0,y+28,465,22,e.Commands.Count+" komend · udźwig "+e.Profile.Cargo+" · ruch "+(e.Profile.MoveTicks/20f).ToString("0.0")+" s/pole",small,Muted);
                if(Btn(0,y+57,108,35,e.Enabled?"Wyłącz":"Włącz",Planning))Change(game.ToggleEcho(e.Id));
                if(Btn(116,y+57,165,35,game.ReplacingId==e.Id?"Anuluj próbę":"Nagraj ponownie",Planning))Change(game.Replace(e.Id));
                if(Btn(289,y+57,182,35,"Zastosuj nowy profil",Planning))Change(game.UpdateEchoProfile(e.Id));
            }
            if(game.Data.Echoes.Count==0)Label(0,0,460,90,"Jeszcze tylko Ty. Ukończ zmianę i zapisz pierwsze Echo.",heading);GUI.EndScrollView();
            Label(0,451,500,58,game.Certificate==null?"Autonomia: nieweryfikowana":"Zweryfikowana: "+game.Certificate.Plates+" płyt / zmianę",body,Cyan);
            if(Btn(0,516,245,46,"Sprawdź autonomię",Planning)){var v=game.Verify();notice=v.Message;if(!v.Passed)tab=3;}
            if(Btn(255,516,245,46,"Uruchom Autoloop",Planning&&game.Certificate!=null,true)){game.Start(true);paused=false;notice="";}
            Label(0,578,500,58,"Aktualizacja profilu zachowuje terminy komend i unieważnia certyfikat. Nowe nagranie pozwala w pełni wykorzystać szybkość.",small,Muted);
        }
        private void Upgrades()
        {
            Label(0,0,500,36,"Rozbudowa hali",heading,Cyan);
            if(Btn(0,48,240,43,"Bufor rudy · 40 C",Planning)){placement=0;newKind=StationKind.Buffer;newMaterial=Material.Ore;notice="Wskaż pole bufora rudy.";}
            if(Btn(255,48,240,43,"Bufor płyt · 40 C",Planning)){placement=0;newKind=StationKind.Buffer;newMaterial=Material.Plate;notice="Wskaż pole bufora płyt.";}
            if(Btn(0,103,495,43,"Dodatkowa prasa · 120 C",Planning)){placement=0;newKind=StationKind.Press;notice="Wskaż pole nowej prasy.";}
            var s=game.Data.Layout.Stations.Find(x=>x.Id==selected);Label(0,167,500,31,"Wybrano: "+(s==null?"brak":s.Name+" #"+s.Id),heading);
            if(s!=null)
            {
                if(Btn(0,210,495,39,"Wejście +2 · "+game.UpgradeCost(selected,UpgradeKind.InputCapacity)+" C",Planning&&s.Kind==StationKind.Press))Change(game.Upgrade(selected,UpgradeKind.InputCapacity));
                if(Btn(0,259,495,39,"Wyjście / bufor +2 · "+game.UpgradeCost(selected,UpgradeKind.OutputCapacity)+" C",Planning&&s.Kind!=StationKind.Dispatch))Change(game.Upgrade(selected,UpgradeKind.OutputCapacity));
                if(Btn(0,308,495,39,"Cykl prasy −1 s · "+game.UpgradeCost(selected,UpgradeKind.ProductionSpeed)+" C",Planning&&s.Kind==StationKind.Press))Change(game.Upgrade(selected,UpgradeKind.ProductionSpeed));
            }
            Label(0,371,500,32,"Profil nowych nagrań",heading);var op=game.Data.Layout.Operator;Label(0,408,500,38,"Udźwig "+op.Cargo+" · ruch "+op.MoveTicks/20f+" s · obsługa "+op.HandlingTicks/20f+" s",small,Muted);
            if(Btn(0,451,495,39,"Udźwig +1 · "+game.UpgradeCost(0,UpgradeKind.Cargo)+" C",Planning))Change(game.Upgrade(0,UpgradeKind.Cargo));
            if(Btn(0,500,495,39,"Ruch −0,1 s/pole · "+game.UpgradeCost(0,UpgradeKind.MoveSpeed)+" C",Planning))Change(game.Upgrade(0,UpgradeKind.MoveSpeed));
            if(Btn(0,549,495,39,"Obsługa −0,2 s · "+game.UpgradeCost(0,UpgradeKind.HandlingSpeed)+" C",Planning))Change(game.Upgrade(0,UpgradeKind.HandlingSpeed));
            Label(0,604,500,34,"Ceny testowe. Podstawowa linia pozostaje darmowa.",small,Muted);
        }
        private void Log()
        {
            var sim=game.Simulation; var first=sim.FirstProblem();
            Label(0,0,500,35,"Dziennik zmiany",heading,Cyan);
            Label(0,40,500,35,"Błędy: "+sim.ErrorCount+" · wpisy: "+sim.Events.Count,small,Muted);
            string diagnosis=first==null?(sim.Finished?"Brak błędów i niedokończonych czynności. Wynik autonomii sprawdź w zakładce Echo.":"Pierwszy problem pojawi się tutaj. Oczekiwanie sprawdzisz w panelu stacji."):
                "Pierwszy problem · "+(first.Tick/(float)Rules.TickRate).ToString("0.0")+" s\n"+first.Context+"\n"+first.Message;
            Label(0,79,500,112,diagnosis,small,first==null?Muted:Amber);
            if(Btn(0,198,242,36,errorsOnly?"Pokaż wszystkie wpisy":"Tylko błędy")){errorsOnly=!errorsOnly;scroll=Vector2.zero;}
            if(Btn(255,198,242,36,"Przejdź do stacji",first!=null&&sim.Station(first.StationId)!=null)){selected=first.StationId;tab=0;scroll=Vector2.zero;}
            var events=sim.Events.Where(e=>!errorsOnly||e.Error).Reverse().Take(100).ToList();
            scroll=GUI.BeginScrollView(new Rect(0,246,510,384),scroll,new Rect(0,0,485,Math.Max(360,events.Count*84)));
            for(int i=0;i<events.Count;i++){var e=events[i];Label(0,i*84,475,81,(e.Tick/(float)Rules.TickRate).ToString("0.0")+" s · "+e.Context+"\n"+e.Message,small,e.Error?Amber:Muted);}
            if(events.Count==0)Label(0,0,480,80,errorsOnly?"Brak wpisów błędów. Niedokończona czynność jest opisana powyżej.":"Zdarzenia pojawią się podczas pracy.");GUI.EndScrollView();
        }
        private void Overlay(string text){Fill(new Rect(0,0,ViewWidth,ViewHeight),new Color(0,0,0,.85f));Fill(new Rect(370,140,700,630),Panel);Label(400,170,640,75,text,heading,Cyan);}
        private void Help()
        {
            Overlay("Naucz fabrykę swojej pracy");Label(400,250,640,325,"1. Rozpocznij zmianę i naciśnij 1, by pobrać rudę.\n\n2. Naciśnij 2, by zanieść ją do magazynu prasy. Możesz od razu wrócić po kolejną rudę.\n\n3. Z pustymi rękami naciśnij 2, aby odebrać płytę, potem 3, aby ją wysłać.\n\n4. Po 60 sekundach zapisz Echo. Następna zmiana odtworzy Twoje zadanie.\n\n5. Podziel pracę między dostawcę i odbiorcę. Przetestuj autonomię w zakładce Echo.");
            Label(400,591,640,81,"Stacje wybierzesz też myszą. Akcje w panelu obejmują dojście i przeładunek. Zakładka Rozwój pozwala dobudować bufory i ulepszać linię.",body,Muted);if(Btn(400,695,640,46,"Wracam do hali",true,true))help=false;
        }
        private void AbortDialog()
        {
            Overlay("Przerwać bieżącą zmianę?");Label(400,270,640,180,"Wynik niepełnej zmiany nie zostanie wypłacony. Zapisane wcześniej Echo i zakupy pozostaną.");
            if(Btn(400,510,305,50,"Przerwij")){game.Abort();paused=false;abort=false;notice="";Save();}if(Btn(720,510,320,50,"Kontynuuj",true,true))abort=false;
        }
        private void Summary()
        {
            Overlay("Zmiana ukończona");var sim=game.Simulation;Label(400,263,640,80,sim.Shipped+" płyt · +"+game.LastEarnings+" Credits · "+sim.ErrorCount+" błędów",heading);
            Label(400,363,640,72,"Zapisz pracę jako Echo albo odrzuć nagranie. W obu przypadkach zachowujesz zarobek.");echoName=GUI.TextField(new Rect(400,456,640,48),echoName,28,field);
            bool has=sim.Operator!=null&&sim.Operator.Commands.Count>0;
            if(Btn(400,538,305,52,game.ReplacingId==0?"Zapisz Echo":"Zastąp Echo",has,true)){if(game.SaveEcho(echoName)){Save();notice="";tab=1;}else notice=game.Message;}
            if(Btn(720,538,320,52,"Odrzuć nagranie")){game.Discard();Save();notice="";}Label(400,630,640,70,notice,body,Amber);
        }
        private void Keys()
        {
            var e=Event.current;if(e.type!=EventType.KeyDown)return;
            if(e.keyCode==KeyCode.Escape){placement=-1;e.Use();return;}if(e.keyCode==KeyCode.Tab){tab=(tab+1)%4;scroll=Vector2.zero;e.Use();return;}if(e.keyCode==KeyCode.Space){paused=!paused;e.Use();return;}
            if(game.Phase!=GamePhase.Recording||paused)return;var u=game.Simulation.Operator;
            if(e.keyCode==KeyCode.Alpha1)Quick(1);else if(e.keyCode==KeyCode.Alpha2)Quick(2);else if(e.keyCode==KeyCode.Alpha3)Quick(3);else if(e.keyCode==KeyCode.E)Quick(selected);
            else{Cell d;if(e.keyCode==KeyCode.W||e.keyCode==KeyCode.UpArrow)d=new Cell(0,-1);else if(e.keyCode==KeyCode.S||e.keyCode==KeyCode.DownArrow)d=new Cell(0,1);else if(e.keyCode==KeyCode.A||e.keyCode==KeyCode.LeftArrow)d=new Cell(-1,0);else if(e.keyCode==KeyCode.D||e.keyCode==KeyCode.RightArrow)d=new Cell(1,0);else return;string why;game.Simulation.EnqueueMove(new Cell(u.Position.X+d.X,u.Position.Y+d.Y),out why);notice=why;}e.Use();
        }
    }
}
