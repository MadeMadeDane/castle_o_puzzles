using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate bool Check();
public class CheckAction
{
    public string item_name;
    public Check check;
    public Action action;
    public CheckAction (string n, Check chk , Action act)
    {
        item_name = n;
        check = chk;
        action = act;
    }

}
public class Item
{
    public static string name = "Item";
    public Type type = typeof(Item);
    public MonoBehaviour ctx;
    protected List<CheckAction> update_check_action_list;
    protected List<CheckAction> fixed_update_check_action_list;

    public GameObject physical_form;
    public Sprite menu_form;
    public GameObject physical_obj = null;
    public GameObject menu_obj = null;

    public bool enable_logs = false;

    public Item()
    {
        update_check_action_list = new List<CheckAction>();
        fixed_update_check_action_list = new List<CheckAction>();
    }

    public virtual void Update()
    {
    }

    public virtual void FixedUpdate()
    {
    }

    public virtual void Start()
    {

    }

    public virtual void Destroy()
    {

    }
    
    protected void outputLogs(string msg)
    {
        if (enable_logs)
            Debug.Log(msg);
    }
    public virtual string GetName()
    {
        return Item.name;
    }
}