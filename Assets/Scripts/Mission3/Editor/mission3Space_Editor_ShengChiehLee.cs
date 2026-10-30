using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Keep the authored three-metre grid while giving this level a wider playable footprint.
public static class mission3Space_Editor_ShengChiehLee
{
    public const float PlanScale=1.15f;
    public const float ClearHeight=4.8f;
    public static Vector3 Expanded(Vector3 point)=>new Vector3(point.x*PlanScale,point.y,point.z*PlanScale);
    static Transform Root(Scene scene,string name)=>scene.GetRootGameObjects().Single(x=>x.name==name).transform;
    static mission3Hotel_Manager_ShengChiehLee Manager(Scene scene)=>scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<mission3Hotel_Manager_ShengChiehLee>(true)).Single();

    public static void Prepare(Scene scene)
    {
        var architecture=Root(scene,"Mission3_HotelArchitecture");var props=Root(scene,"Mission3_PropsAndInteractions");
        float previous=architecture.localScale.x;
        if(Mathf.Abs(previous-1)>.001f&&Mathf.Abs(previous-PlanScale)>.001f)throw new Exception("Unexpected architecture scale; preserve the manual layout before upgrading.");
        var player=Manager(scene).playerBody.transform;var p=player.position;
        player.position=new Vector3(p.x/previous,p.y,p.z/previous);
        architecture.localScale=props.localScale=Vector3.one;
    }

    public static void Apply(Scene scene)
    {
        var architecture=Root(scene,"Mission3_HotelArchitecture");var props=Root(scene,"Mission3_PropsAndInteractions");var manager=Manager(scene);
        foreach(var t in architecture.GetComponentsInChildren<Transform>(true))
        {
            if(t.name.StartsWith("Ceiling_")||t.name=="Service ceiling")SetHeight(t,ClearHeight+.1f,.2f);
            else if(t.name.StartsWith("Wall_")||t.name=="Service north jamb"||t.name=="Service south jamb")SetHeight(t,ClearHeight/2,ClearHeight);
            else if(t.name=="Ceiling fixture")SetHeight(t,ClearHeight-.12f,.12f);
            var light=t.GetComponent<Light>();if(light!=null){SetY(t,ClearHeight-.45f);light.range=13;light.intensity=3.3f;}
        }
        // The service opening and lift rise with the hotel rather than leaving low obstructions.
        SetHeight(manager.serviceDoor.transform,ClearHeight/2,ClearHeight);
        var lift=props.GetComponentInChildren<mission3Lift_Prop_ShengChiehLee>(true);
        foreach(var t in lift.transform.Cast<Transform>())
        {
            if(t.name=="Cabin back wall"||t.name=="Cabin left wall"||t.name=="Cabin right wall"||t.name=="Left door frame"||t.name=="Right door frame")SetLocalHeight(t,ClearHeight/2,ClearHeight);
            else if(t.name=="Cabin ceiling"||t.name=="Top door frame")SetLocalY(t,ClearHeight+.05f);
            else if(t.name=="Left sliding door"||t.name=="Right sliding door")SetLocalHeight(t,ClearHeight/2,ClearHeight-.12f);
            else if(t.name=="Floor indicator"||t.name=="Lift floor number")SetLocalY(t,ClearHeight-.36f);
            else if(t.name=="Cabin light")SetLocalY(t,ClearHeight-.3f);
        }
        RelocateItems(props);
        architecture.localScale=props.localScale=new Vector3(PlanScale,1,PlanScale);
        Root(scene,"Mission3_DemoLandmarks").localScale=new Vector3(PlanScale,1,PlanScale);
        manager.playerBody.transform.position=Expanded(manager.playerBody.transform.position);
        manager.canvasUI.mapWorldMin=new Vector2(-1.5f,-61.5f)*PlanScale;
        manager.canvasUI.mapWorldMax=new Vector2(61.5f,1.5f)*PlanScale;
        var lounge=Root(scene,"Mission3_DemoLandmarks").GetComponentsInChildren<Light>(true).Single();SetY(lounge.transform,ClearHeight-.45f);lounge.range=13; lounge.intensity=2.6f;
        Physics.SyncTransforms();
    }
    static void RelocateItems(Transform props)
    {
        var items=props.GetComponentsInChildren<mission3Interact_Prop_ShengChiehLee>(true).Where(x=>x.kind==mission3Interact_Prop_ShengChiehLee.Kind.LostProperty).OrderBy(x=>x.index).ToArray();
        var displays=props.GetComponentsInChildren<Transform>(true).Where(x=>x.name.StartsWith("Lost property display")).ToList();
        // Each display sits against a room boundary, outside both the doorway and through route.
        var positions=new[]{new Vector3(18,1.04f,-29.15f),new Vector3(57.85f,1.04f,-34.5f),new Vector3(2.15f,1.33f,-33.6f),new Vector3(36,1.04f,-48.85f),new Vector3(36,1.04f,-15.85f),new Vector3(18,1.04f,-48.85f)};
        var yaw=new[]{0f,90f,-90f,180f,180f,180f};
        foreach(var item in items)
        {
            var shelf=displays.OrderBy(x=>(x.position-item.transform.position).sqrMagnitude).First();displays.Remove(shelf);
            int i=item.index;shelf.name="Lost property display / "+(i+1).ToString("00");
            shelf.position=new Vector3(positions[i].x,.5f,positions[i].z);shelf.rotation=Quaternion.Euler(0,yaw[i],0);shelf.localScale=new Vector3(1.1f,1,.60f);
            item.transform.position=positions[i];item.transform.rotation=Quaternion.Euler(0,yaw[i],0);
            // Raycastable pickup volumes should never catch the player's capsule.
            var hit=item.GetComponent<BoxCollider>();hit.isTrigger=true;hit.size=new Vector3(.88f,.8f,.66f);
        }
    }
    static void SetY(Transform t,float y){var p=t.position;p.y=y;t.position=p;}
    static void SetHeight(Transform t,float y,float height){SetY(t,y);var s=t.localScale;s.y=height;t.localScale=s;}
    static void SetLocalY(Transform t,float y){var p=t.localPosition;p.y=y;t.localPosition=p;}
    static void SetLocalHeight(Transform t,float y,float height){SetLocalY(t,y);var s=t.localScale;s.y=height;t.localScale=s;}

    public static void Validate(Scene scene)
    {
        var manager=Manager(scene);var architecture=Root(scene,"Mission3_HotelArchitecture");
        if(Mathf.Abs(architecture.lossyScale.x-PlanScale)>.001f)throw new Exception("Hotel footprint was not expanded.");
        Physics.SyncTransforms();
        var capsule=manager.playerBody.GetComponent<CapsuleCollider>();var playerScale=capsule.transform.lossyScale;
        float radius=capsule.radius*Mathf.Max(playerScale.x,playerScale.z),height=Mathf.Max(capsule.height*playerScale.y,2*radius);
        float eyes=height/2+.03f+manager.playerCamera.transform.position.y-manager.playerBody.position.y;
        foreach(var ceiling in architecture.GetComponentsInChildren<Transform>(true).Where(x=>x.name.StartsWith("Ceiling_")||x.name=="Service ceiling"))
            if(Mathf.Abs(ceiling.GetComponent<Collider>().bounds.min.y-ClearHeight)>.01f)throw new Exception("Ceiling height mismatch: "+ceiling.name);
        foreach(var item in manager.canvasUI.itemPositions)
        {
            if(!item.GetComponent<Collider>().isTrigger)throw new Exception("Pickup blocks player movement: "+item.name);
            // Approach the display from its open face at normal eye height.
            var front=item.position-item.forward*(1.5f*PlanScale);front.y=eyes;
            var direction=item.position-front;
            var hits=Physics.RaycastAll(front,direction.normalized,3.4f,~0,QueryTriggerInteraction.Collide).Where(x=>x.collider.gameObject.scene==scene).OrderBy(x=>x.distance).ToArray();
            if(hits.Length==0||hits[0].collider.GetComponentInParent<mission3Interact_Prop_ShengChiehLee>()!=item.GetComponent<mission3Interact_Prop_ShengChiehLee>())throw new Exception("Pickup sightline blocked: "+item.name+" by "+(hits.Length==0?"nothing":hits[0].collider.name));
            var low=new Vector3(front.x,radius+.03f,front.z);var high=new Vector3(front.x,height-radius+.03f,front.z);
            var blocked=Physics.OverlapCapsule(low,high,radius,~0,QueryTriggerInteraction.Ignore).Where(x=>x.gameObject.scene==scene&&!x.transform.IsChildOf(manager.playerBody.transform)).ToArray();
            if(blocked.Length>0)throw new Exception("Display approach blocked: "+item.name+"; scene="+scene.path+"; item="+item.position+"; approach="+front+"; radius="+radius+" by "+string.Join(", ",blocked.Select(x=>x.name+" "+x.bounds)));
        }
    }
}
