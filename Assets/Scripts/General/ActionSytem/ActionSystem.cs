using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine;


public class ActionSystem : Singleton<ActionSystem>
{
    private List<GameAction> reactions = null;
    public bool IsPerforming { get; private set; } = false;
    private static Dictionary<(Type, ReactionTiming, Delegate), Action<GameAction>> wrappedDelegates = new();
    private static Dictionary<Type, List<Action<GameAction>>> preSubs = new();
    private static Dictionary<Type, List<Action<GameAction>>> postSubs = new();
    private static Dictionary<Type, Func<GameAction, IEnumerator>> performers = new();
    public void Perform(GameAction action, System.Action OnPerformFinished = null)
    {
        if (BattleSystem.Instance != null && BattleSystem.Instance.IsBattleOver) return;
        if (IsPerforming) return;
        IsPerforming = true;
        StartCoroutine(Flow(action, () =>
        {
            IsPerforming = false;
            OnPerformFinished?.Invoke();
        }));
    }


    public void AddReaction(GameAction gameAction)
    {
        reactions?.Add(gameAction);
    }
    private IEnumerator Flow(GameAction action, Action OnFlowFInished = null)
    {
        if (BattleSystem.Instance != null && BattleSystem.Instance.IsBattleOver)
        {
            yield break;
        }
        reactions = action.PreReactions;
        PerformSubscribers(action, preSubs);
        yield return PerformReactions();

        reactions = action.PerformReactions;
        yield return PerformPerformer(action);
        yield return PerformReactions();

        reactions = action.PostReactions;
        PerformSubscribers(action, postSubs);
        yield return PerformReactions();

        OnFlowFInished?.Invoke();
    }

    private IEnumerator PerformPerformer(GameAction action)
    {
        Type type = action.GetType();
        if(performers.ContainsKey(type))
        {
            yield return performers[type](action);
        }
    }

    private void PerformSubscribers(GameAction action, Dictionary<Type, List<Action<GameAction>>> subs)
    {
        Type type = action.GetType();
        if(subs.ContainsKey(type))
        {
            foreach (var sub in subs[type])
            {
                sub(action);
            }
        }
    }
    private IEnumerator PerformReactions()
    {
        foreach(var reaction in reactions)
        {
            yield return Flow(reaction);    
        }
    }


    public static void AttachPerformer<T>(Func<T, IEnumerator> performer) where T : GameAction
    {
        Type type = typeof(T);
        IEnumerator wrappedPerformer(GameAction action) => performer((T)action);
        if (performers.ContainsKey(type)) performers[type] = wrappedPerformer;
        else performers.Add(type, wrappedPerformer);   
    }
    public static void DetachPerformer<T>() where T : GameAction
    {
        Type type = typeof(T);
        if (performers.ContainsKey(type)) performers.Remove(type);
    }

    public static void SubscribeReaction<T>(Action<T> reaction, ReactionTiming timing) where T : GameAction
    {
        Dictionary<Type, List<Action<GameAction>>> subs = timing == ReactionTiming.PRE ? preSubs : postSubs;
        var key = (typeof(T), timing, (Delegate)reaction);
        if (wrappedDelegates.ContainsKey(key)) return;
        void wrappedReaction(GameAction action) => reaction((T)action);
        wrappedDelegates.Add(key, wrappedReaction);

        if (subs.ContainsKey(typeof(T)))
        {
            subs[typeof(T)].Add(wrappedReaction);
        }
        else
        {
            subs.Add(typeof(T), new());
            subs[typeof(T)].Add(wrappedReaction);

        }
    }
    public static void UnsubscribeReaction<T>(Action<T> reaction, ReactionTiming timing) where T : GameAction
    {
        Dictionary<Type, List<Action<GameAction>>> subs = timing == ReactionTiming.PRE ? preSubs : postSubs;
        var key = (typeof(T), timing, (Delegate)reaction);
        if(wrappedDelegates.TryGetValue(key, out var wrappedReaction))
        {
            if (subs.ContainsKey(typeof(T)))
            {
                subs[typeof(T)].Remove(wrappedReaction);
            }
            wrappedDelegates.Remove(key);
        }

    }
}



