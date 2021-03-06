﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class txUIObject : ComponentOwner
{
	protected UI_TYPE mType = UI_TYPE.UT_BASE;
	protected AudioSource mAudioSource;
	protected Transform mTransform;
	protected BoxCollider mBoxCollider;
	protected UIWidget mWidget;
	protected static int mIDSeed = 0;
	protected bool mPassRay = true;
	protected bool mMouseHovered = false;
	protected txUIObject mParent;
	protected List<txUIObject> mChildList;
	public GameLayout mLayout;
	public GameObject mObject;
	public int mID;
	public txUIObject()
		:
		base("")
	{
		mID = mIDSeed++;
		mChildList = new List<txUIObject>();
	}
	public override void destroy()
	{
		base.destroy();
		base.destroyAllComponents();
		destroyWindow(this);
	}
	protected static void destroyWindow(txUIObject window)
	{
		// 先销毁所有子节点
		int childCount = window.mChildList.Count;
		for(int i = 0; i < childCount; ++i)
		{
			destroyWindow(window.mChildList[i]);
		}
		// 再销毁自己
		if(window.mLayout != null)
		{
			window.mLayout.unregisterUIObject(window);
			window.mLayout = null;
		}
		UnityUtility.destroyGameObject(window.mObject);
		window.mObject = null;
	}
	public virtual void init(GameLayout layout, GameObject go, txUIObject parent)
	{
		mLayout = layout;
		setGameObject(go);
		setParent(parent);
		initComponents();
		if (mLayout != null)
		{
			mLayout.registerUIObject(this);
		}
		mAudioSource = mObject.GetComponent<AudioSource>();
		mBoxCollider = mObject.GetComponent<BoxCollider>();
		mWidget = mObject.GetComponent<UIWidget>();
		if (mBoxCollider != null && mLayout.isCheckBoxAnchor())
		{
			string layoutName = "";
			if(mLayout != null)
			{
				layoutName = mLayout.getName();
			}
			// BoxCollider必须与UIWidget(或者UIWidget的派生类)一起使用,否则在自适应屏幕时BoxCollider会出现错误
			if (mWidget == null)
			{
				logError("BoxCollider must used with UIWidget! Otherwise can not adapt to the screen sometimes! name : " + mName + ", layout : " + layoutName);
			}
			else if(!mWidget.autoResizeBoxCollider)
			{
				logError("UIWidget's autoResizeBoxCollider must be true! Otherwise can not adapt to the screen sometimes! name : " + mName + ", layout : " + layoutName);
			}
			// BoxCollider的中心必须为0,因为UIWidget会自动调整BoxCollider的大小和位置,而且调整后位置为0,所以在制作时BoxCollider的位置必须为0
			if(!isFloatZero(mBoxCollider.center.sqrMagnitude))
			{
				logError("BoxCollider's center must be zero! Otherwise can not adapt to the screen sometimes! name : " + mName + ", layout : " + layoutName);
			}
			if(mObject.GetComponent<ScaleAnchor>() == null)
			{
				logError("Window with BoxCollider and Widget must has ScaleAnchor! Otherwise can not adapt to the screen sometimes! name : " + mName + ", layout : " + layoutName);
			}
		}
	}
	public override void initComponents()
	{
		addComponent<WindowComponentAudio>("Audio");
		addComponent<WindowComponentRotateSpeed>("RotateSpeed");
		addComponent<WindowComponentMove>("Move");
		addComponent<WindowComponentScale>("Scale");
		addComponent<WindowComponentAlpha>("Alpha");
		addComponent<WindowComponentRotate>("Rotate");
		addComponent<WindowComponentSlider>("slider");
		addComponent<WindowComponentFill>("fill");
		addComponent<WindowComponentRotateFixed>("RotateFixed");
		addComponent<WindowComponentHSL>("HSL");
		addComponent<WindowComponentLum>("Lum");
		addComponent<WindowComponentDrag>("Drag");
		addComponent<WindowComponentTrackTarget>("TrackTarget");
	}
	public void addChild(txUIObject child)
	{
		if(!mChildList.Contains(child))
		{
			mChildList.Add(child);
		}
	}
	public void removeChild(txUIObject child)
	{
		if (mChildList.Contains(child))
		{
			mChildList.Remove(child);
		}
	}
	public AudioSource createAudioSource()
	{
		mAudioSource = mObject.AddComponent<AudioSource>();
		return mAudioSource;
	}
	public virtual void update(float elapsedTime)
	{
		base.updateComponents(elapsedTime);
	}
	//get
	//-------------------------------------------------------------------------------------------------------------------------------------
	public List<txUIObject> getChildList() { return mChildList; }
	public txUIObject getParent() { return mParent; }
	public UI_TYPE getUIType() { return mType; }
	public Transform getTransform() { return mTransform; }
	public AudioSource getAudioSource() { return mAudioSource; }
	public bool isActive() { return mObject.activeSelf; }
	public BoxCollider getBoxCollider(bool addIfNull = false)
	{
		if (mBoxCollider == null && addIfNull)
		{
			mBoxCollider = mObject.AddComponent<BoxCollider>();
		}
		return mBoxCollider;
	}
	public Vector3 getRotationEuler()
	{
		Vector3 vector3 = mTransform.localEulerAngles;
		adjustAngle180(ref vector3.z);
		return vector3;
	}
	public Vector3 getRotationRadian()
	{
		Vector3 vector3 = mTransform.localEulerAngles * 0.0055f;
		adjustRadian180(ref vector3.z);
		return vector3;
	}
	public virtual Vector3 getPosition() { return mTransform.localPosition; }
	public virtual Vector3 getWorldPosition() { return mTransform.position; }
	public Vector2 getScale() { return new Vector2(mTransform.localScale.x, mTransform.localScale.y); }
	public Vector2 getWorldScale()
	{
		Vector3 scale = getMatrixScale(mTransform.localToWorldMatrix);
		txUIObject root = mLayout.isNGUI() ? mLayoutManager.getNGUIRoot() : mLayoutManager.getUGUIRoot();
		Vector3 uiRootScale = root.getTransform().localScale;
		return new Vector2(scale.x / uiRootScale.x, scale.y / uiRootScale.y);
	}
	public int getChildCount() { return mTransform.childCount; }
	public GameObject getChild(int index) { return mTransform.GetChild(index).gameObject; }
	public virtual float getAlpha() { return 1.0f; }
	public virtual float getFillPercent() { return 1.0f; }
	public virtual int getDepth() { return 0; }
	public virtual bool getHandleInput(){return mBoxCollider != null && mBoxCollider.enabled;}
	public bool getPassRay() { return mPassRay; }
	public bool getMouseHovered() { return mMouseHovered; }
	//set
	//-------------------------------------------------------------------------------------------------------------------------------------
	public void setParent(txUIObject parent)
	{
		if (mParent == parent)
		{
			return;
		}
		// 从原来的父节点上移除
		if (mParent != null)
		{
			mParent.removeChild(this);
		}
		// 设置新的父节点
		mParent = parent;
		if (parent != null)
		{
			parent.addChild(this);
			if (mTransform.parent != parent.mObject.transform)
			{
				mTransform.SetParent(parent.mObject.transform);
			}
		}
	}
	protected void setGameObject(GameObject go)
	{
		setName(go.name);
		mObject = go;
		mTransform = mObject.transform;
	}
	public override void setName(string name)
	{
		base.setName(name);
		if (mObject != null && mObject.name != name)
		{
			mObject.name = name;
		}
	}
	public virtual void setDepth(int depth)
	{
		mGlobalTouchSystem.notifyButtonDepthChanged(this, depth);
	}
	public virtual void setHandleInput(bool enable)
	{
		if(mBoxCollider != null)
		{
			mBoxCollider.enabled = enable;
		}
	}
	public void setActive(bool active) { mObject.SetActive(active); }
	public void setLocalScale(Vector2 scale) { mTransform.localScale = new Vector3(scale.x, scale.y, 1.0f); }
	public virtual void setLocalPosition(Vector3 pos) { mTransform.localPosition = pos; }
	public void setLocalRotation(Vector3 rot) { mTransform.localEulerAngles = rot; }
	public void setWorldRotation(Vector3 rot) { mTransform.eulerAngles = rot; }
	public virtual void setWorldPosition(Vector3 pos) { mTransform.position = pos; }
	public virtual void setAlpha(float alpha) { }
	public virtual void setFillPercent(float percent) { }
	public void setPassRay(bool pass) { mPassRay = pass; }
	public void setMouseHovered(bool hover) { mMouseHovered = hover; }
	public void setClickCallback(UIEventListener.VoidDelegate callback){UIEventListener.Get(mObject).onClick = callback;}
	public void setHoverCallback(UIEventListener.BoolDelegate callback){UIEventListener.Get(mObject).onHover = callback;}
	public void setPressCallback(UIEventListener.BoolDelegate callback){UIEventListener.Get(mObject).onPress = callback;}
	// callback
	//--------------------------------------------------------------------------------------------------------------------------------
	public virtual void onMouseEnter(){}
	public virtual void onMouseLeave(){}
	// 鼠标左键在窗口内按下
	public virtual void onMouseDown(Vector2 mousePos) { }
	// 鼠标左键在窗口内放开
	public virtual void onMouseUp(Vector2 mousePos) { }
	// 鼠标在窗口内,并且有移动
	public virtual void onMouseMove(Vector2 mousePos, Vector2 moveDelta, float moveSpeed) { }
	// 鼠标在窗口内,但是不移动
	public virtual void onMouseStay(Vector2 mousePos) { }
}