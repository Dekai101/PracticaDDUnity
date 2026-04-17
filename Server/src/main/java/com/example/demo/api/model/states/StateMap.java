package com.example.demo.api.model.states;

import java.util.concurrent.TimeUnit;

import com.example.demo.api.model.Room;
import com.example.demo.api.model.messages.JSONMessage;
import com.example.demo.api.model.messages.in.select_room.SelectRoomMessage_IN;
import com.example.demo.api.model.messages.out.show_map.ShowMapMessage_OUT;
import com.example.demo.components.GameInstance;

import tools.jackson.databind.ObjectMapper;

public class StateMap extends State
{
    private ObjectMapper mapper;

    private ShowMapMessage_OUT msg;

    public StateMap(GameInstance game) {
        super(game);
        mapper = new ObjectMapper();

        game.createNextRooms();

        msg = new ShowMapMessage_OUT();
        msg.rooms = game.getNextRooms();

        JSONMessage gm = new JSONMessage(game.getId(), msg);
        game.broadcast(gm);
    }


    @Override
    public void tick() {
        game.pollMessage(1, TimeUnit.MILLISECONDS);

        //TO DO
    }


    private void select_room(Room r, JSONMessage jsonmsg){
        SelectRoomMessage_IN message = mapper.treeToValue(jsonmsg.data, SelectRoomMessage_IN.class);

    }
}