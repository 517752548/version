﻿using UnityEngine;
using System.Collections;
using GameLogic.Game.Perceptions;
using Layout.LayoutElements;
using GameLogic.Game.Elements;
using GameLogic.Game.LayoutLogics;
using Vector3 = UnityEngine.Vector3;
using System.Collections.Generic;
using EngineCore;
using EngineCore.Simulater;
using Layout.AITree;


public class UPerceptionView :XSingleton<UPerceptionView>,IBattlePerception {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public UGameScene UScene;

	public bool UseCache = true;

	void Awake()
	{
		_magicData = new Dictionary<string, Layout.MagicData> ();
		_timeLines = new Dictionary<string, TimeLine> ();
		var  magics = ResourcesManager.Singleton.LoadAll<TextAsset> ("Magics");
		foreach (var i in magics) 
        {           
			var m = XmlParser.DeSerialize<Layout.MagicData> (i.text);
			_magicData.Add (m.key, m);
		}
		magicCount = _magicData.Count;
		var timeLines = ResourcesManager.Singleton.LoadAll<TextAsset> ("Layouts");
		foreach (var i in timeLines) 
		{
			var line = XmlParser.DeSerialize<TimeLine> (i.text);
			_timeLines.Add ("Layouts/" + i.name+".xml",line);
		}
		timeLineCount = _timeLines.Count;

		UScene = GameObject.FindObjectOfType<UGameScene> ();
	}

	public int timeLineCount = 0;
	public int magicCount =0;


	#region IBattlePerception implementation

    public void SetPercetion(BattlePerception per)
    {

        //Not implementation
    }

	public GVector3 GetBornPosByTeamIndex(int teamIndex)
	{
		if (teamIndex == 1) {
			return GTransform.Convent (UScene.tower.position);
		} else {
			return GTransform.Convent (UScene.towerEnemy.position);
		}
	}

	public EngineCore.GVector3 GetStartPoint ()
	{
		var start = UScene.startPoint;
		return new EngineCore.GVector3 (start.position.x, start.position.y, start.position.y);
	}


    public void ProcessDamage(IBattleCharacter view1, IBattleCharacter view2, GameLogic.Game.DamageResult result)
    {
       
    }

	public EngineCore.GVector3 GetEnemyStartPoint ()
	{
		var start = UScene.enemyStartPoint;
		return new EngineCore.GVector3 (start.position.x, start.position.y, start.position.y);
	}

	public  TimeLine GetTimeLineByPath (string path)
	{
		if (UseCache)
		{
			TimeLine line;
			if (_timeLines.TryGetValue (path, out line)) {
				return line;	
			}
			Debug.LogError ("No found timeline by path:" + path);
		} 
		return TryToLoad (path);
	}

	private TimeLine TryToLoad(string path)
	{
		var lineAsset = ResourcesManager.Singleton.LoadText(path);
		if (string.IsNullOrEmpty(lineAsset))
			return null;
		
		var line = XmlParser.DeSerialize<TimeLine> (lineAsset);
		if (UseCache) 
		{
			_timeLines.Add (path, line);
		} 
		return line;
	}

	private Dictionary<string,TimeLine> _timeLines;

	private Dictionary<string ,Layout.MagicData> _magicData;

	public Layout.MagicData GetMagicByKey (string key)
	{
		Layout.MagicData magic;
		if(_magicData.TryGetValue(key,out magic))
		{
			return magic;	
		}
		Debug.LogError ("No found magic by key:"+key);
		return null;
	}

	public bool ExistMagicKey (string key)
	{
		return _magicData.ContainsKey (key);
	}

	public IBattleCharacter CreateBattleCharacterView (string resources, GVector3 pos,GVector3 forward)
	{
		var character = ResourcesManager.Singleton.LoadResourcesWithExName<GameObject> (resources);
		var tPos = new Vector3(pos.x,pos.y,pos.z);
		var qu = Quaternion.Euler (forward.x, forward.y, forward.z);
		var ins = GameObject.Instantiate (character) as GameObject;
		var root = new GameObject (resources);
		root.transform.position = tPos;
		root.transform.rotation = Quaternion.identity;
		ins.transform.parent = root.transform;
		ins.transform.localPosition = Vector3.zero;
		ins.transform.localRotation =  Quaternion.identity;
		ins.name = "Character";
		var view= root.AddComponent<UCharacterView> ();
		view.targetLookQuaternion = qu;
		view.SetCharacter(ins);

		return view;

	}

	public IMagicReleaser CreateReleaserView (GameLogic.Game.Elements.IBattleCharacter releaser, GameLogic.Game.Elements.IBattleCharacter targt, EngineCore.GVector3? targetPos)
	{
		var obj = new GameObject ("MagicReleaser");
		return obj.AddComponent<UMagicReleaserView> ();
	}
        

	public IBattleMissile CreateMissile (GameLogic.Game.Elements.IMagicReleaser releaser, Layout.LayoutElements.MissileLayout layout)
	{
		var viewRelease = releaser as UMagicReleaserView;
		var viewTarget = viewRelease.CharacterTarget as UCharacterView;
		var characterView = viewRelease.CharacterReleaser as UCharacterView;
		var res = layout.resourcesPath;
		var obj = ResourcesManager.Singleton.LoadResourcesWithExName<GameObject> (res);
		GameObject ins;
		if (obj == null) {
			ins = new GameObject ("Missile");
		} else {
			ins = GameObject.Instantiate (obj);
		}
		var trans = (GTransform)characterView.Transform ;
		var offset =  trans.Rotation* new Vector3(layout.offset.x,layout.offset.y,layout.offset.z);
		ins.transform.position = characterView.GetBoneByName (layout.fromBone).position+offset;
		ins.transform.rotation = Quaternion.identity;

		var missile = ins.AddComponent<UBattleMissileView> (); //NO
		var path = ins.GetComponent<MissileFollowPath> ();
		if(path)
		   path.SetTarget(viewTarget.GetBoneByName(layout.toBone),layout.speed);
		return missile;
	}

    public IParticlePlayer CreateParticlePlayer(IMagicReleaser releaser, ParticleLayout layout)
    {
        var res = layout.path;
        var obj = ResourcesManager.Singleton.LoadResourcesWithExName<GameObject> (res);
        GameObject ins;
        if (obj == null) {
            return null;
        } else {
            ins = GameObject.Instantiate (obj);
        }
        var viewRelease = releaser as UMagicReleaserView;
        var viewTarget = viewRelease.CharacterTarget as UCharacterView;
        var characterView = viewRelease.CharacterReleaser as UCharacterView;
       // var trans = (GTransform)characterView.Transform ;
        var form = layout.fromTarget == Layout.TargetType.Releaser ? characterView : viewTarget;
        if (layout.Bind)
        {
            ins.transform.SetParent(form.GetBoneByName(layout.fromBoneName),false);
        }
        else
        {
            ins.transform.position = form.GetBoneByName(layout.fromBoneName).position;
            ins.transform.rotation = Quaternion.identity;
        }

        switch (layout.destoryType)
        {
            case  ParticleDestoryType.Time:
                GameObject.Destroy(ins, layout.destoryTime);
                break;
            case ParticleDestoryType.Normal:
                GameObject.Destroy(ins, 3);
                break;
            case ParticleDestoryType.LayoutTimeOut:
                GameObject.Destroy(ins, 1);
                break;
        }

        return ins.AddComponent<UParticlePlayer>();
    }

	public float Distance (EngineCore.GVector3 v, EngineCore.GVector3 v2)
	{
		var uV = new Vector3 (v.x, v.y, v.z);
		var uV2 = new Vector3 (v2.x, v2.y, v2.z);
		return Vector3.Distance (uV, uV2);
	}

	public float Angle (EngineCore.GVector3 v, EngineCore.GVector3 v2)
	{
		var uV = new Vector3 (v.x, v.y, v.x);
		var uV2 = new Vector3 (v2.x, v2.y, v2.z);

		return Vector3.Angle (uV, uV2);
	}

	public EngineCore.GVector3 RotateWithY (EngineCore.GVector3 v, float angle)
	{
		var uV = new Vector3 (v.x, v.y, v.x);
		var qu = Quaternion.Euler (new Vector3 (0, angle, 0));
		var r = qu*uV;
		return new EngineCore.GVector3 (r.x, r.y, r.z);
	}

	public GVector3 NormalVerctor (GVector3 v)
	{
		var r = new Vector3 (v.x, v.y, v.x).normalized;
		return new EngineCore.GVector3 (r.x, r.y, r.z);
	}

	public  ITimeSimulater GetTimeSimulater ()
	{
		return UAppliaction.Singleton.GetGate ();
	}

	public Layout.AITree.TreeNode GetAITree (string pathTree)
	{
		var xml = ResourcesManager.Singleton.LoadText (pathTree);
		var root = XmlParser.DeSerialize<TreeNode> (xml);
		return root;
	}
	#endregion
}
