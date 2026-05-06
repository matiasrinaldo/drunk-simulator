using System.Collections.Generic;
using UnityEngine;

public class CommandQueue : MonoBehaviour
{
    private readonly Queue<ICommand> queue = new Queue<ICommand>();

    public void Enqueue(ICommand command)
    {
        if (command == null) return;
        queue.Enqueue(command);
    }

    void Update()
    {
        if (queue.Count == 0) return;
        ICommand next = queue.Dequeue();
        next.Execute();
    }
}
