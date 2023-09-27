using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace AnimeNeckChop
{
    public class NeckChop : ThunderScript
    {
        static List<NeckChopCreature> creatures = new List<NeckChopCreature>();
        public class NeckChopCreature
        {
            public BrainData BrainData { get; set; }
            public float Time { get; set; }
            public float VerticalFOV { get; set; }
            public float HorizontalFOV { get; set; }
            public float SpeakLoudness { get; set; }
            public float AudioVolume { get; set; }
        }
        public override void ScriptEnable()
        {
            base.ScriptEnable();
            EventManager.onCreatureHit += EventManager_onCreatureHit;
            EventManager.onCreatureKill += EventManager_onCreatureKill;
        }
        public override void ScriptDisable()
        {
            base.ScriptDisable();
            EventManager.onCreatureHit -= EventManager_onCreatureHit;
            EventManager.onCreatureKill -= EventManager_onCreatureKill;
        }
        private void EventManager_onCreatureKill(Creature creature, Player player, CollisionInstance collisionInstance, EventTime eventTime)
        {
            creature.brain.RemoveNoStandUpModifier(this);
        }
        private void EventManager_onCreatureHit(Creature creature, CollisionInstance collisionInstance)
        {
            if(!creature.isPlayer && !creature.isKilled && Player.local.creature.ragdoll.parts.Contains(collisionInstance.sourceColliderGroup?.collisionHandler?.ragdollPart) && 
                (collisionInstance.sourceColliderGroup?.collisionHandler?.ragdollPart.type == RagdollPart.Type.LeftHand || 
                collisionInstance.sourceColliderGroup?.collisionHandler?.ragdollPart.type == RagdollPart.Type.RightHand) &&
                collisionInstance.targetColliderGroup?.collisionHandler?.ragdollPart.type == RagdollPart.Type.Neck &&
                collisionInstance.damageStruct.hitBack)
            {
                creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                creature.ragdoll.SetState(Ragdoll.State.Inert);
                creature.brain.currentTarget = null;
                creature.brain.SetState(Brain.State.Idle);
                BrainModuleDetection detection = creature.brain.instance.GetModule<BrainModuleDetection>(false);
                BrainModuleSpeak speak = creature.brain.instance.GetModule<BrainModuleSpeak>(false);
                if (!creatures.Exists(match => match.BrainData == creature.brain.instance))
                {
                    NeckChopCreature enemy = new NeckChopCreature { BrainData = creature.brain.instance, Time = Time.time };
                    if (detection != null)
                    {
                        detection.alertednessLevel = 0;
                        detection.canHear = false;
                        enemy.VerticalFOV = detection.sightDetectionVerticalFov;
                        enemy.HorizontalFOV = detection.sightDetectionHorizontalFov;
                        detection.sightDetectionVerticalFov = 0;
                        detection.sightDetectionHorizontalFov = 0;
                    }
                    if (speak != null)
                    {
                        enemy.SpeakLoudness = speak.speakLoudness;
                        enemy.AudioVolume = speak.audioVolume;
                        speak.speakLoudness = 0;
                        speak.audioVolume = 0;
                    }
                    creatures.Add(enemy);
                }
                creature.brain.AddNoStandUpModifier(this);
            }
            else if(!creature.isPlayer && !creature.isKilled && collisionInstance.damageStruct.damage > 0)
            {
                if(creatures.Exists(match => match.BrainData == creature.brain.instance))
                {
                    NeckChopCreature enemy = creatures.Find(match => match.BrainData == creature.brain.instance);
                    if(collisionInstance.sourceColliderGroup != collisionInstance.targetColliderGroup || (collisionInstance.sourceColliderGroup == collisionInstance.targetColliderGroup && Time.time - 5 >= enemy.Time) || collisionInstance.casterHand != null)
                    {
                        if (creature.ragdoll.state == Ragdoll.State.Inert)
                            creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                        creature.brain.RemoveNoStandUpModifier(this); 
                        BrainModuleDetection detection = creature.brain.instance.GetModule<BrainModuleDetection>(false);
                        BrainModuleSpeak speak = creature.brain.instance.GetModule<BrainModuleSpeak>(false);
                        if (detection != null)
                        {
                            detection.sightDetectionVerticalFov = enemy.VerticalFOV;
                            detection.sightDetectionHorizontalFov = enemy.HorizontalFOV;
                        }
                        if (speak != null)
                        {
                            speak.speakLoudness = enemy.SpeakLoudness;
                            speak.audioVolume = enemy.AudioVolume;
                        }
                        creatures.Remove(enemy);
                    }
                }
                else if(collisionInstance.sourceColliderGroup != collisionInstance.targetColliderGroup || collisionInstance.casterHand != null)
                {
                    if (creature.ragdoll.state == Ragdoll.State.Inert)
                        creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                    creature.brain.RemoveNoStandUpModifier(this);
                }
            }
        }
    }
}
