using TMPro;
using UnityEngine;
public static class IntroRoomFactory
{
    static Sprite tile;
    static Sprite Tile(){
        if(!tile){var t=Texture2D.whiteTexture;tile=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f),t.width);}
        return tile;
    }
    static GameObject Art(Transform parent,string name,Vector2 pos,Vector2 size,Sprite sprite,Color tint,int order)
    {
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=pos;
        var sr=g.AddComponent<SpriteRenderer>();sr.sprite=sprite?sprite:Tile();sr.color=sprite?Color.white:tint;sr.sortingOrder=order;
        var bounds=sr.sprite.bounds.size;g.transform.localScale=new Vector3(size.x/bounds.x,size.y/bounds.y,1);return g;
    }
    static void Label(Transform room,string label,Vector2 pos)
    {
        var g=new GameObject(label);g.transform.SetParent(room,false);g.transform.localPosition=pos;
        var t=g.AddComponent<TextMeshPro>();t.text=label;t.alignment=TextAlignmentOptions.Center;t.fontSize=2.8f;t.color=Color.white;
        t.GetComponent<MeshRenderer>().sortingOrder=4;
    }
    static Animator Animate(GameObject g,RuntimeAnimatorController c)
    { var a=g.AddComponent<Animator>();a.runtimeAnimatorController=c;return a; }
    public static IntroKitchenRoom Build(Transform root,IntroContent c)
    {
        var room=root.gameObject.AddComponent<IntroKitchenRoom>();room.content=c;
        Art(root,"Kitchen floor",Vector2.zero,new Vector2(16,9),c.floor,new Color(.29f,.22f,.19f),-3);
        Vector2[] walls={new Vector2(-7.9f,0),new Vector2(7.9f,0),new Vector2(0,4.4f),new Vector2(0,-4.4f)};
        for(int i=0;i<4;i++){
            var wall=Art(root,"Wall",walls[i],i<2?new Vector2(.2f,9):new Vector2(16,.2f),null,new Color(.47f,.32f,.23f),-1);wall.AddComponent<BoxCollider2D>();
        }
        var player=new GameObject("Intro player");player.transform.SetParent(root,false);player.transform.localPosition=new Vector3(0,-1,0);
        room.hero=player.AddComponent<Rigidbody2D>();room.hero.gravityScale=0;room.hero.freezeRotation=true;player.AddComponent<CircleCollider2D>().radius=.32f;
        var art=Art(player.transform,"Player art",Vector2.zero,new Vector2(.7f,.9f),c.hero,new Color(.55f,.83f,.65f),2);
        room.heroAnimator=Animate(art,c.heroAnimations);art.AddComponent<IntroStationPulse>();
        var pig=Art(root,"Sleeping pig",new Vector2(5,-2),new Vector2(1,.6f),c.sleepingPig,new Color(.94f,.59f,.62f),1);
        Animate(pig,c.pigAnimations);pig.AddComponent<IntroStationPulse>().breathing=true;Label(root,"z z z",new Vector2(5,-1.35f));
        room.stations=new Transform[7];room.stationAnimators=new Animator[7];
        for(int i=0;i<7;i++){
            var pos=i==6?new Vector2(5,1):new Vector2(-5+3*(i%3),i<3?2.6f:.25f);
            var sprite=i==6?c.oven:i<3?c.pairs[i].rawSprite:c.pairs[i-3].machineSprite;
            var station=Art(root,room.StationName(i),pos,Vector2.one,sprite,i==6?new Color(.9f,.46f,.23f):i<3?new Color(.74f,.6f,.32f):new Color(.42f,.6f,.63f),1);
            station.AddComponent<BoxCollider2D>();station.AddComponent<IntroStationPulse>();room.stations[i]=station.transform;
            room.stationAnimators[i]=Animate(station,c.stationAnimations);Label(root,room.StationName(i),pos+Vector2.up*.76f);
        }
        room.effects=root.gameObject.AddComponent<AudioSource>();room.effects.playOnAwake=false;return room;
    }
}
