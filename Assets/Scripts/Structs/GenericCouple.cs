using System;

[Serializable]
public struct GenericCouple<T1, T2>
{
    public T1 First;
    public T2 Second;

    public GenericCouple(T1 inFirst, T2 inSecond)
    {
        First = inFirst;
        Second = inSecond;
    }
}
