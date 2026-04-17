package com.example.demo.api.model.messages.in.character_action;

import com.example.demo.api.model.messages.MessageBody;

public class CharacterActionMessage_IN extends MessageBody{

    public final static String TYPE = "CHARACTER_ACTION";

    @Override
    public String getMessageType(){
        return TYPE;
    }

    public int action_type;
    
}