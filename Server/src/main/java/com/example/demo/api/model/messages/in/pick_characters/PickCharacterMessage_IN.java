package com.example.demo.api.model.messages.in.pick_characters;

import com.example.demo.api.model.messages.MessageBody;

public class PickCharacterMessage_IN extends MessageBody{

    public final static String TYPE = "PICK_PLAYERS";
    
    @Override
    public String getMessageType(){
        return TYPE;
    }

    public int playerId;
    public int characterId;
}
