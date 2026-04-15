package com.example.demo.api.model.messages.in.selectroom;

import com.example.demo.api.model.messages.MessageBody;

public class SelectRoomMessage_IN extends MessageBody{

    public final static String TYPE = "SELECT_ROOM";

    @Override
    public String getMessageType(){
        return TYPE;
    }

    public int room_type;
    
}