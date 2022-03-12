﻿using System;
using System.Collections.Generic;
using System.Linq;
using MVCF.Commands;
using MVCF.Comps;
using MVCF.VerbComps;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MVCF.Reloading.Comps
{
    public class VerbComp_Reloadable : VerbComp
    {
        public int ShotsRemaining;
        public VerbCompProperties_Reloadable Props => props as VerbCompProperties_Reloadable;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ShotsRemaining, "shotsRemaining");
        }

        public override IEnumerable<CommandPart> GetCommandParts(Command_VerbTargetExtended command)
        {
            yield return new CommandPart_Reloadable
            {
                parent = command,
                Reloadable = this
            };
        }

        public virtual Thing Reload(Thing ammo)
        {
            if (!CanReloadFrom(ammo)) return null;
            var shotsToFill = ShotsToReload(ammo);
            ShotsRemaining += shotsToFill;
            return ammo.SplitOff(shotsToFill * Props.ItemsPerShot);
        }

        public virtual int ReloadTicks(Thing ammo) => ammo == null ? 0 : (Props.ReloadTimePerShot * ShotsToReload(ammo)).SecondsToTicks();
        private int ShotsToReload(Thing ammo) => Math.Min(ammo.stackCount / Props.ItemsPerShot, Props.MaxShots - ShotsRemaining);

        public virtual bool NeedsReload() => ShotsRemaining < Props.MaxShots;

        public virtual bool CanReloadFrom(Thing ammo)
        {
            // Log.Message(ammo + " x" + ammo.stackCount);
            if (ammo == null) return false;
            return Props.AmmoFilter.Allows(ammo) && ammo.stackCount >= Props.ItemsPerShot;
        }

        public virtual void Unload()
        {
            var thing = ThingMaker.MakeThing(Props.AmmoFilter.AnyAllowedDef);
            thing.stackCount = ShotsRemaining;
            ShotsRemaining = 0;
            GenPlace.TryPlaceThing(thing, parent.Verb.caster.Position, parent.Verb.caster.Map, ThingPlaceMode.Near);
        }

        public virtual void ReloadEffect(int curTick, int ticksTillDone)
        {
            if (curTick == ticksTillDone - 2f.SecondsToTicks()) Props.ReloadSound?.PlayOneShot(parent.Verb.caster);
        }

        public override void Initialize(VerbCompProperties props)
        {
            base.Initialize(props);
            ShotsRemaining = Props.StartLoaded ? Props.MaxShots : 0;
        }

        public override void Notify_ShotFired()
        {
            base.Notify_ShotFired();
            ShotsRemaining--;
        }

        public override bool Available() => ShotsRemaining >= (parent.Verb.Bursting ? 0 : parent.Verb.verbProps.burstShotCount);
    }

    public class CommandPart_Reloadable : CommandPart
    {
        public VerbComp_Reloadable Reloadable;

        public override void PostInit()
        {
            base.PostInit();
            if (Reloadable.ShotsRemaining < parent.verb.verbProps.burstShotCount)
                parent.Disable("CommandReload_NoAmmo".Translate("ammo".Named("CHARGENOUN"),
                    Reloadable.Props.AmmoFilter.AnyAllowedDef.Named("AMMO"),
                    ((Reloadable.Props.MaxShots - Reloadable.ShotsRemaining) * Reloadable.Props.ItemsPerShot).Named("COUNT")));
        }

        public override void ModifyInfo(ref string label, ref string topRightLabel, ref string desc, ref Texture2D icon)
        {
            base.ModifyInfo(ref label, ref topRightLabel, ref desc, ref icon);
            topRightLabel = Reloadable.ShotsRemaining + " / " + Reloadable.Props.MaxShots;
        }

        public override IEnumerable<FloatMenuOption> GetRightClickOptions()
        {
            if (Reloadable is VerbComp_Reloadable_ChangeableAmmo ccwa)
                foreach (var option in ccwa.AmmoOptions.Select(pair =>
                    new FloatMenuOption(pair.First.LabelCap, pair.Second)))
                    yield return option;
        }
    }

    public class VerbCompProperties_Reloadable : VerbCompProperties
    {
        public ThingFilter AmmoFilter;
        public List<ThingDefCountRangeClass> GenerateAmmo;
        public bool GenerateBackupWeapon;
        public int ItemsPerShot;
        public int MaxShots;
        public Type NewVerbClass;
        public SoundDef ReloadSound;
        public float ReloadTimePerShot;
        public bool StartLoaded = true;

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            AmmoFilter.ResolveReferences();
        }

        public override void PostLoad(VerbProperties verbProps, AdditionalVerbProps additionalProps)
        {
            base.PostLoad(verbProps, additionalProps);
            Base.EnabledFeatures.Add("Reloading");
            Base.EnabledFeatures.Add("VerbComps");
            Base.EnabledFeatures.Add("ExtraEquipmentVerbs");
            ref var type = ref verbProps.verbClass;
            if (NewVerbClass != null) type = NewVerbClass;
        }
    }
}