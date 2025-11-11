
-- PROIECT BDM 
-- Gestiunea resurselor multimedia (imagini + audio)

-- TABELA PENTRU IMAGINI

create table img_pasari (
  img_id number primary key,
  nume_comun varchar2(120),
  specie varchar2(150),
  familie varchar2(120),
  habitat varchar2(120),
  img_obj ordsys.ordimage,
  img_sign ordsys.ordimagesignature,
  tip_fisier varchar2(60),
  creat_la date default sysdate
);


-- TABELA PENTRU FISIERE AUDIO

create table audio_pasari (
  aud_id number primary key,
  titlu varchar2(200),
  nume_comun varchar2(120),
  specie varchar2(150),
  blob_data blob,
  tip_fisier varchar2(60),
  durata_sec number,
  creat_la date default sysdate
);


-- GENERARE SEMNATURA PT O IMAGINE

create or replace procedure generate_signature_img(p_img_id in number) is
  v_img  ordsys.ordimage;
  v_sign ordsys.ordimagesignature;
begin
  select img_obj, img_sign
  into v_img, v_sign
  from img_pasari
  where img_id = p_img_id
  for update;

  if v_sign is null then
    v_sign := ordsys.ordimagesignature.init();
  end if;

  v_sign.generatesignature(v_img);

  update img_pasari
  set img_sign = v_sign
  where img_id = p_img_id;

  commit;
exception
  when others then
    rollback;
    raise;
end;
/


-- INSERARE IMAGINE

create or replace procedure add_img (
  p_img_id     in number,
  p_nume_comun in varchar2,
  p_specie     in varchar2,
  p_familie    in varchar2,
  p_habitat    in varchar2,
  p_blob       in blob,
  p_tip        in varchar2
) is
begin
  insert into img_pasari (
    img_id, nume_comun, specie, familie, habitat,
    img_obj, img_sign, tip_fisier
  )
  values (
    p_img_id, p_nume_comun, p_specie, p_familie, p_habitat,
    ordsys.ordimage(p_blob, 1), ordsys.ordimagesignature.init(), p_tip
  );

  commit;
end;
/

-- CITIRE / AFISARE IMAGINE

create or replace procedure getcontent_img (
  p_img_id in number,
  p_blob out blob
) is
  v_obj ordsys.ordimage;
begin
  select img_obj into v_obj
  from img_pasari
  where img_id = p_img_id;

  p_blob := v_obj.getcontent();
end;
/


-- PRELUCRARE IMAGINI 

-- Redimensionare

create or replace procedure resize_img(
  p_img_id in number,
  p_w      in number,
  p_h      in number
) is
  v_img ordsys.ordimage;
begin
  select img_obj into v_img
  from img_pasari
  where img_id = p_img_id
  for update;

  v_img.process('fixedScale=' || p_w || ' ' || p_h);
  v_img.setproperties();

  update img_pasari
     set img_obj = v_img
   where img_id = p_img_id;

  commit;
end;
/

-- Rotire
create or replace procedure rotate_img(
  p_img_id in number,
  p_angle in number
) is
  v_img ordsys.ordimage;
begin
  select img_obj into v_img
  from img_pasari
  where img_id = p_img_id
  for update;

  v_img.process('rotate=' || p_angle);
  v_img.setproperties();

  update img_pasari
     set img_obj = v_img
   where img_id = p_img_id;

  commit;
end;
/

-- Ajustare luminozitate
create or replace procedure adjust_brightness_img(
  p_img_id in number,
  p_gamma  in number
) is
  v_img ordsys.ordimage;
begin
  select img_obj into v_img
  from img_pasari
  where img_id = p_img_id
  for update;

  v_img.process('gamma=' || p_gamma);
  v_img.setproperties();

  update img_pasari
     set img_obj = v_img
   where img_id = p_img_id;

  commit;
end;
/


-- EXPORT IMAGINE

create or replace procedure export_img (
  p_img_id in number,
  p_blob out blob
) is
  v_img ordsys.ordimage;
begin
  select img_obj into v_img
  from img_pasari
  where img_id = p_img_id;

  p_blob := v_img.getcontent();
end;
/


-- RECUNOASTERE SEMANTICĂ

-- Citire imagine
create or replace procedure psreadimage_pasari (
  vid in number,
  flux out blob
) is
  obj ordsys.ordimage;
begin
  select img_obj into obj
  from img_pasari
  where img_id = vid;

  flux := obj.getcontent();
end;
/

-- Generare semnaturi pentru toate imaginile
create or replace procedure psgen_semn_pasari is
  myimg  ordsys.ordimage;
  mysig  ordsys.ordimagesignature;
begin
  for x in (select img_id from img_pasari) loop
    select s.img_obj, s.img_sign
    into myimg, mysig
    from img_pasari s
    where s.img_id = x.img_id
    for update;

    if mysig is null then
      mysig := ordsys.ordimagesignature.init();
    end if;

    mysig.generatesignature(myimg);

    update img_pasari s
       set s.img_sign = mysig
     where s.img_id = x.img_id;
  end loop;

  commit;
end;
/

-- Recunoastere imagine similara
create or replace procedure psregasire_pasari (
  fis       in blob,
  cculoare  in decimal,
  ctextura  in decimal,
  cforma    in decimal,
  clocatie  in decimal,
  idrez     out integer,
  p_scor    out number
) is
  scor    number;
  qimg    ordsys.ordimage;
  qsemn   ordsys.ordimagesignature;
  mysemn  ordsys.ordimagesignature;
  mymin   number := 100;
begin
  qimg := ordsys.ordimage(fis, 1);
  qimg.setproperties;

  qsemn := ordsys.ordimagesignature.init();
  dbms_lob.createtemporary(qsemn.signature, true);
  qsemn.generatesignature(qimg);

  for x in (select img_id, img_sign from img_pasari where img_sign is not null) loop
    mysemn := x.img_sign;

    scor := ordsys.ordimagesignature.evaluatescore(
               qsemn, mysemn,
               'color=' || cculoare ||
               ' texture=' || ctextura ||
               ' shape=' || cforma ||
               ' location=' || clocatie);

    if scor < mymin then
      mymin := scor;
      idrez := x.img_id;
    end if;
  end loop;

  p_scor := mymin;
end;
/


-- GESTIONAREA RESURSELOR AUDIO

-- Inserare fisier audio
create or replace procedure add_audio (
  p_aud_id in number,
  p_titlu in varchar2,
  p_nume_comun in varchar2,
  p_specie in varchar2,
  p_blob in blob,
  p_tip in varchar2,
  p_durata in number
) is
begin
  insert into audio_pasari (
    aud_id, titlu, nume_comun, specie,
    blob_data, tip_fisier, durata_sec
  )
  values (
    p_aud_id, p_titlu, p_nume_comun, p_specie,
    p_blob, p_tip, p_durata
  );
  commit;
end;
/

-- Citire fisier audio
create or replace procedure get_audio (
  p_aud_id in number,
  p_blob out blob
) is
  v_blob blob;
begin
  select blob_data into v_blob
  from audio_pasari
  where aud_id = p_aud_id;

  p_blob := v_blob;
end;
/





