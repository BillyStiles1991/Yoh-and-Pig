using System;
using System.Linq;
using System.Collections.Generic;
namespace FreezerGameplay
{
    // No second relationship balance: legacy bond JSON is only a migration input.
    public static class NpcReviewRules
    {
        public static void Migrate(GameProgress data)
        {
            if(data.reviews==null)data.reviews=new List<NpcReviewRecord>();
            if(data.bonds==null)data.bonds=new List<NpcBondRecord>();
            if(data.reviewVersion<1)
            {
                foreach(var old in data.bonds)
                {
                    if(old==null||string.IsNullOrWhiteSpace(old.npcId))continue;
                    if(data.reviews.Any(r=>r!=null&&r.npcId==old.npcId))continue;
                    data.reviews.Add(new NpcReviewRecord{npcId=old.npcId,points=Math.Max(0,old.bond),lastMealDay=old.lastMealDay,masterpieceGiven=old.masterpieceGiven});
                }
                data.reviewVersion=1;data.bonds.Clear();
            }
        }
        public static NpcReviewRecord Get(GameProgress data,string id)
        {
            Migrate(data);var record=data.reviews.Find(r=>r.npcId==id);
            if(record==null){record=new NpcReviewRecord{npcId=id};data.reviews.Add(record);}return record;
        }
        public static bool ValidThresholds(NpcProfileDefinition profile)
        {
            var p=profile?.reviewLevelPoints;if(p==null||p.Length==0||p[0]!=0)return false;
            for(int i=1;i<p.Length;i++)if(p[i]<=p[i-1])return false;return true;
        }
        public static int Level(NpcProfileDefinition profile,int points)
        {
            if(!ValidThresholds(profile))return 1;
            int level=1;for(int i=1;i<profile.reviewLevelPoints.Length&&points>=profile.reviewLevelPoints[i];i++)level=i+1;return level;
        }
        public static bool RefreshUnlocks(NpcReviewRecord record,NpcProfileDefinition profile)
        {
            int rank=Level(profile,record.points);bool a=record.keyNpcUnlocked,b=record.animatedBannerUnlocked,c=record.customFontUnlocked;
            record.keyNpcUnlocked|=profile.startsAsKeyNpc||rank>=profile.keyNpcLevel;
            record.animatedBannerUnlocked|=rank>=profile.animatedBannerLevel;
            record.customFontUnlocked|=rank>=profile.customFontLevel;
            return a!=record.keyNpcUnlocked||b!=record.animatedBannerUnlocked||c!=record.customFontUnlocked;
        }
        public static bool Award(GameProgress data,NpcProfileDefinition profile,int points,string source,bool promote=false)
        {
            if(profile==null||!ValidThresholds(profile)||string.IsNullOrWhiteSpace(source))return false;
            var r=Get(data,profile.id);if(r.awardedSources.Contains(source))return false;
            r.points=(int)Math.Max(0L,Math.Min(int.MaxValue,(long)r.points+points));
            if(promote)
            {
                int index=Math.Max(0,Math.Min(profile.reviewLevelPoints.Length-1,profile.keyNpcLevel-1));
                r.points=Math.Max(r.points,profile.reviewLevelPoints[index]);r.keyNpcUnlocked=true;
            }
            r.awardedSources.Add(source);RefreshUnlocks(r,profile);return true;
        }
    }
}
