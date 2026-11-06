using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// Mission3's night attendant. Mission2 supplies the capsule silhouette; its flashlight/death rules are not reused.
public class mission3Monster_Manager_ShengChiehLee : MonoBehaviour
{
    public enum Behaviour { Dormant, Warning, Patrol, Investigate, Chase, Search, Recovering, Retired }
    public mission3Hotel_Manager_ShengChiehLee manager;
    public NavMeshAgent agent;
    public GameObject body;
    public Transform[] earlyRoute,expandedRoute,spawnPoints;
    public Bounds lobbySafeBounds;
    public TMP_Text status;
    public GameObject statusCard;
    public CanvasGroup recoveryShade;
    public AudioSource footsteps;
    public Light[] warningLights;
    public float patrolSpeed=2,chaseSpeed=6,visionDistance=12,hearingDistance=10;
    public float warningSeconds=3,loseSightSeconds=4,searchSeconds=6,catchDistance=1.05f;
    public Behaviour State { get; private set; }
    public bool IsRecovering=>State==Behaviour.Recovering;
    public bool ExpandedPatrol=>manager!=null&&manager.CollectedCount>=4;
    public int TimesCaught { get; private set; }
    public Vector3 LastKnownPosition { get; private set; }
    public bool PlayerIsSafe=>manager!=null&&(lobbySafeBounds.Contains(manager.playerBody.position)||manager.IsInsideLift);
    public bool CanSeePlayer=>manager!=null&&Visible(manager.playerBody.position+Vector3.up*.25f,true);
    public bool NavigationReady=>agent!=null&&agent.enabled&&agent.isOnNavMesh;
    public float StateTimeRemaining=>remaining;
    public float ProtectionSecondsRemaining=>Mathf.Max(0,protectedUntil-Time.time);
    float remaining,lostSight,repath,stepTimer,protectedUntil;
    float[] lightIntensities;
    int waypoint;
    bool wasPaused,announcedExpansion;
    Vector3 lobbyReturn;
    AudioClip[] footstepClips;
    NavMeshPath hearingPath;
    AudioClip generatedStep;

    void Start()
    {
        State=Behaviour.Dormant;agent.enabled=false;body.SetActive(false);
        lobbyReturn=manager.playerBody.position;lightIntensities=warningLights.Select(x=>x==null?0:x.intensity).ToArray();
        footstepClips=manager.movement.footstepClips;hearingPath=new NavMeshPath();
        if(footstepClips==null||footstepClips.Length==0)
        {
            var samples=new float[5512];
            for(int i=0;i<samples.Length;i++){float t=i/22050f;samples[i]=Mathf.Exp(-t*28)*(Mathf.Sin(t*2*Mathf.PI*65)*.65f+Random.Range(-.15f,.15f));}
            generatedStep=AudioClip.Create("Night attendant / generated low footstep",samples.Length,1,22050,false);generatedStep.SetData(samples,0);footstepClips=new[]{generatedStep};
        }
        recoveryShade.alpha=0;recoveryShade.gameObject.SetActive(false);RefreshHUD();
    }
    void Update()
    {
        if(manager==null)return;
        if(manager.ReturnsSubmitted||manager.IsCompleted)
        {
            State=Behaviour.Retired;StopAgent();body.SetActive(false);RestoreLights();RefreshHUD();return;
        }
        if(IsRecovering){RefreshHUD();return;}
        if(manager.CurrentPage!=mission3Hotel_Manager_ShengChiehLee.Page.None)
        {
            wasPaused=true;StopAgent();RestoreLights();RefreshHUD();return;
        }
        if(wasPaused){wasPaused=false;protectedUntil=Mathf.Max(protectedUntil,Time.time+1);}
        if(State==Behaviour.Dormant&&manager.CollectedCount>=2)
        {
            State=Behaviour.Warning;remaining=warningSeconds;
            manager.ShowHint("A night attendant is waking. Walk quietly. Break sight to escape. Reception and the lift are safe.");
        }
        if(ExpandedPatrol&&!announcedExpansion&&State!=Behaviour.Dormant)
        {
            announcedExpansion=true;manager.ShowHint("The night patrol now covers the east wing and lounge. Use the hotel loops to escape.");
        }
        if(State==Behaviour.Warning)
        {
            remaining-=Time.deltaTime;Flicker();PlayStep(.62f);
            if(remaining<=0){RestoreLights();if(TrySpawn()){State=Behaviour.Patrol;waypoint=0;}else remaining=.5f;}
            RefreshHUD();return;
        }
        if(State==Behaviour.Dormant){RefreshHUD();return;}
        if(!NavigationReady){if(!TrySpawn()){RefreshHUD();return;}}
        if(PlayerIsSafe)
        {
            if(State==Behaviour.Chase||State==Behaviour.Search||State==Behaviour.Investigate){State=Behaviour.Patrol;repath=0;}
            Patrol();RefreshHUD();return;
        }
        if(Time.time>=protectedUntil&&FlatDistance(transform.position,manager.playerBody.position)<=catchDistance&&Visible(manager.playerBody.position,false))
        {
            StartCoroutine(ReturnToLobby());return;
        }
        bool sees=CanSeePlayer;
        if(sees&&Time.time>=protectedUntil)
        {
            if(State!=Behaviour.Chase)manager.ShowHint("SPOTTED. Run and turn corners to break the attendant's line of sight.");
            State=Behaviour.Chase;LastKnownPosition=manager.playerBody.position;lostSight=0;
        }
        else if(State==Behaviour.Chase)
        {
            lostSight+=Time.deltaTime;
            if(lostSight>=loseSightSeconds){State=Behaviour.Search;remaining=searchSeconds;repath=0;}
        }
        if(State!=Behaviour.Chase&&Time.time>=protectedUntil&&CanHearRunning())
        {
            State=Behaviour.Investigate;LastKnownPosition=manager.playerBody.position;remaining=5;repath=0;
        }
        if(State==Behaviour.Chase)GoTo(LastKnownPosition,chaseSpeed);
        else if(State==Behaviour.Investigate||State==Behaviour.Search)
        {
            remaining-=Time.deltaTime;GoTo(LastKnownPosition,patrolSpeed*1.35f);
            if(remaining<=0){State=Behaviour.Patrol;repath=0;}
        }
        else Patrol();
        if(agent.velocity.sqrMagnitude>.2f)PlayStep(State==Behaviour.Chase?.32f:.65f);
        RefreshHUD();
    }
    public bool CanHearRunning()
    {
        if(PlayerIsSafe||manager.CurrentPage!=mission3Hotel_Manager_ShengChiehLee.Page.None)return false;
        var velocity=manager.playerBody.linearVelocity;velocity.y=0;
        if(Keyboard.current==null||!Keyboard.current.leftShiftKey.isPressed||velocity.magnitude<manager.movement.walkSpeed+.4f)return false;
        float distance=FlatDistance(transform.position,manager.playerBody.position);
        bool clear=Visible(manager.playerBody.position,false);
        if(distance>hearingDistance*(clear?1:.5f))return false;
        if(!NavMesh.SamplePosition(manager.playerBody.position,out var destination,1.7f,NavMesh.AllAreas))return false;
        if(!NavMesh.CalculatePath(transform.position,destination.position,NavMesh.AllAreas,hearingPath)||hearingPath.status!=NavMeshPathStatus.PathComplete)return false;
        float length=0;for(int i=1;i<hearingPath.corners.Length;i++)length+=Vector3.Distance(hearingPath.corners[i-1],hearingPath.corners[i]);
        return length<=hearingDistance*1.5f;
    }
    bool Visible(Vector3 target,bool useCone)
    {
        Vector3 origin=transform.position+Vector3.up*2.1f,direction=target-origin;
        if(useCone&&(direction.magnitude>visionDistance||Vector3.Angle(transform.forward,new Vector3(direction.x,0,direction.z))>55))return false;
        foreach(var hit in Physics.RaycastAll(origin,direction.normalized,direction.magnitude+.1f,~0,QueryTriggerInteraction.Ignore).OrderBy(x=>x.distance))
        {
            if(hit.transform.IsChildOf(transform))continue;
            return hit.transform.IsChildOf(manager.playerBody.transform);
        }
        return true;
    }
    bool TrySpawn()
    {
        foreach(var point in spawnPoints.OrderByDescending(x=>FlatDistance(x.position,manager.playerBody.position)))
        {
            if(FlatDistance(point.position,manager.playerBody.position)<12||!NavMesh.SamplePosition(point.position,out var hit,1.3f,NavMesh.AllAreas))continue;
            var to=hit.position+Vector3.up*1.8f-manager.playerCamera.transform.position;
            bool inView=Vector3.Angle(manager.playerCamera.transform.forward,to)<65;
            bool blocked=Physics.Linecast(manager.playerCamera.transform.position,hit.position+Vector3.up*1.8f,~0,QueryTriggerInteraction.Ignore);
            if(inView&&!blocked)continue;
            agent.enabled=false;transform.position=hit.position;agent.enabled=true;
            if(!agent.isOnNavMesh){agent.enabled=false;continue;}
            body.SetActive(true);repath=0;return true;
        }
        return false;
    }
    void Patrol()
    {
        var route=ExpandedPatrol?expandedRoute:earlyRoute;if(route.Length==0)return;
        waypoint%=route.Length;
        if(!agent.pathPending&&FlatDistance(transform.position,route[waypoint].position)<1)waypoint=(waypoint+1)%route.Length;
        GoTo(route[waypoint].position,patrolSpeed);
    }
    void GoTo(Vector3 destination,float speed)
    {
        if(!NavigationReady)return;
        agent.isStopped=false;agent.speed=speed;repath-=Time.deltaTime;
        if(repath>0)return;repath=.25f;
        if(!lobbySafeBounds.Contains(destination)&&NavMesh.SamplePosition(destination,out var hit,1.5f,NavMesh.AllAreas))agent.SetDestination(hit.position);
    }
    public void PageChanged()
    {
        if(manager!=null&&manager.CurrentPage!=mission3Hotel_Manager_ShengChiehLee.Page.None){wasPaused=true;StopAgent();RestoreLights();}
    }
    void StopAgent(){if(NavigationReady){agent.isStopped=true;agent.velocity=Vector3.zero;}}
    void PlayStep(float interval)
    {
        stepTimer-=Time.deltaTime;if(stepTimer>0)return;stepTimer=interval;
        if(footsteps!=null&&footstepClips!=null&&footstepClips.Length>0){footsteps.spatialBlend=State==Behaviour.Warning?0:1;footsteps.PlayOneShot(footstepClips[Random.Range(0,footstepClips.Length)],.65f);}
    }
    void Flicker(){for(int i=0;i<warningLights.Length;i++)if(warningLights[i]!=null)warningLights[i].intensity=lightIntensities[i]*(Mathf.Sin(Time.time*23)>0?.45f:1);}
    void RestoreLights(){if(lightIntensities==null)return;for(int i=0;i<warningLights.Length;i++)if(warningLights[i]!=null)warningLights[i].intensity=lightIntensities[i];}
    IEnumerator ReturnToLobby()
    {
        State=Behaviour.Recovering;TimesCaught++;StopAgent();body.SetActive(false);RestoreLights();
        recoveryShade.gameObject.SetActive(true);recoveryShade.alpha=1;
        manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.Pause);
        manager.canvasUI.pages[8].SetActive(false);
        yield return new WaitForSecondsRealtime(.65f);
        manager.playerBody.position=lobbyReturn;manager.playerBody.transform.position=lobbyReturn;Physics.SyncTransforms();
        yield return new WaitForSecondsRealtime(.55f);
        float fade=0;while(fade<.5f){fade+=Time.unscaledDeltaTime;recoveryShade.alpha=1-fade/.5f;yield return null;}
        recoveryShade.gameObject.SetActive(false);agent.enabled=false;
        State=Behaviour.Patrol;protectedUntil=Time.time+8;manager.SetPage(mission3Hotel_Manager_ShengChiehLee.Page.None);
        manager.ShowHint("Returned to reception. Your belongings, notes and owner choices are safe. Take a breath, then try another route.");
    }
    void RefreshHUD()
    {
        if(statusCard!=null)statusCard.SetActive(manager.CurrentPage==mission3Hotel_Manager_ShengChiehLee.Page.None&&!IsRecovering);
        if(status==null)return;
        bool safe=PlayerIsSafe;
        status.text=State==Behaviour.Retired?"NIGHT PATROL / SHIFT FINISHED":safe?"SAFE AREA / RECEPTION + LIFT":
            State==Behaviour.Dormant?"NIGHT PATROL / QUIET FOR NOW":State==Behaviour.Warning?"FOOTSTEPS / THE ATTENDANT IS WAKING":
            State==Behaviour.Chase?"SPOTTED / RUN + BREAK LINE OF SIGHT":State==Behaviour.Search||State==Behaviour.Investigate?"SEARCHING / KEEP QUIET + CHANGE ROUTE":
            (ExpandedPatrol?"NIGHT PATROL / ALL WINGS":"NIGHT PATROL / WEST CORRIDORS");
        status.color=safe?new Color(.55f,.87f,.77f):State==Behaviour.Chase?new Color(1,.4f,.29f):new Color(.97f,.81f,.48f);
    }
    static float FlatDistance(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
    void OnDisable(){RestoreLights();}
    void OnDestroy(){if(generatedStep!=null)Destroy(generatedStep);}
}
