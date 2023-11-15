using Godot;
using System;

public partial class debug_server : Control
{
    [Signal]
    public delegate void PlayerPositionChangedEventHandler(Vector3 new_position);

    public void player_position_changed(Vector3 new_position)
    {
        EmitSignal(SignalName.PlayerPositionChanged, new_position);
    }
}
