using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
namespace FreezerGameplay
{
    [Serializable] public class ClimateVisual { public string id; public GameObject visual; }
    public class RegionalClimate : MonoBehaviour
    {
        public InventoryController inventory; public string regionId="kitchen";
        public ClimateVisual[] seasonVisuals=new ClimateVisual[0],weatherVisuals=new ClimateVisual[0];
        public UnityEvent<string> seasonChanged=new UnityEvent<string>(),weatherChanged=new UnityEvent<string>();
        public string Season {get;private set;} public string Weather {get;private set;}
        string lastRegion;int lastDay=-1;
        public void SetRegion(string id){regionId=id;lastDay=-1;Refresh();}
        void Update(){if(inventory)Refresh();}
        public static int StableHash(string s){unchecked{int n=17;foreach(char c in s??"")n=n*31+c;return n;}}
        static int Pick(float[] weights, System.Random rng)
        {
            double total=weights.Sum(w=>(double)Mathf.Max(0,w));if(total<=0)return 0;
            double roll=rng.NextDouble()*total;for(int i=0;i<weights.Length;i++){roll-=Mathf.Max(0,weights[i]);if(roll<0)return i;}return weights.Length-1;
        }
        public static RegionYearRecord Generate(GameProgress g,RegionalClimateDefinition region,int year)
        {
            int progress=region.storyProgressEvents.Count(g.triggeredEvents.Contains);
            var choices=region.seasons.Where(s=>s!=null&&s.minimumStoryProgress<=progress&&s.weight>0).ToArray();
            if(choices.Length==0)throw new InvalidOperationException("Region needs an eligible season: "+region.id);
            var rng=new System.Random(unchecked(g.worldSeed^StableHash(region.id)^year*7919));
            float[] counts=Enumerable.Range(0,8).Select(i=>i<region.seasonCountWeights.Length?region.seasonCountWeights[i]:0).ToArray();
            int count=1+Pick(counts,rng);
            var record=new RegionYearRecord{regionId=region.id,year=year};
            int[] lengths=Enumerable.Repeat(1,count).ToArray();for(int i=count;i<112;i++)lengths[rng.Next(count)]++;
            int day=0;string previous="";
            for(int i=0;i<count;i++)
            {
                var pool=choices.Length>1?choices.Where(s=>s.id!=previous).ToArray():choices;
                if(pool.Length==0)pool=choices;
                var season=pool[Pick(pool.Select(s=>s.weight).ToArray(),rng)];
                record.seasons.Add(new RegionalSeasonRecord{seasonId=season.id,startDay=day,length=lengths[i]});previous=season.id;
                for(int d=0;d<lengths[i];d++)
                {
                    var weather=season.weather.Where(w=>w!=null&&w.weight>0).ToArray();
                    record.weather.Add(weather.Length==0?"clear":weather[Pick(weather.Select(w=>w.weight).ToArray(),rng)].id);
                }
                day+=lengths[i];
            }
            return record;
        }
        public void Refresh()
        {
            var g=inventory.store.Data;var definition=inventory.database.regions.FirstOrDefault(r=>r!=null&&r.id==regionId);if(definition==null)return;
            int day=inventory.store.Day-1,year=day/112,within=day%112;
            var record=g.regionalYears.Find(r=>r.regionId==regionId&&r.year==year);
            if(record==null){record=Generate(g,definition,year);g.regionalYears.Add(record);}
            foreach(var regional in inventory.database.regions)foreach(var rule in regional.storyRules)
                if(!string.IsNullOrEmpty(rule.eventId)&&g.triggeredEvents.Contains(rule.eventId)&&!g.climateOverrides.Any(x=>x.regionId==regional.id&&x.ruleId==rule.id))
                    g.climateOverrides.Add(new ClimateOverrideRecord{regionId=regional.id,ruleId=rule.id,seasonId=rule.seasonId,weatherId=rule.weatherId,startTime=g.daySeconds,endTime=g.daySeconds+Math.Max(1,rule.durationSeconds>0?rule.durationSeconds:rule.durationDays*g.secondsPerDay)});
            var active=g.climateOverrides.Where(x=>x.regionId==regionId&&g.daySeconds>=x.startTime&&g.daySeconds<x.endTime)
                .OrderByDescending(x=>definition.storyRules.FirstOrDefault(r=>r.id==x.ruleId)?.priority??0).FirstOrDefault();
            string seasonId=record.seasons.First(s=>within>=s.startDay&&within<s.startDay+s.length).seasonId;
            string weatherId=record.weather[within];
            if(active!=null){if(!string.IsNullOrEmpty(active.seasonId))seasonId=active.seasonId;if(!string.IsNullOrEmpty(active.weatherId))weatherId=active.weatherId;}
            bool changed=lastRegion!=regionId;
            if(Season!=seasonId||changed){Season=seasonId;foreach(var v in seasonVisuals)if(v.visual)v.visual.SetActive(v.id==seasonId);seasonChanged.Invoke(seasonId);}
            if(Weather!=weatherId||changed){Weather=weatherId;foreach(var v in weatherVisuals)if(v.visual)v.visual.SetActive(v.id==weatherId);weatherChanged.Invoke(weatherId);}
            lastRegion=regionId;lastDay=day;
        }
    }
}
