/****************  SHIPMENTS BY CUSTOMER REPORT EMAIL W/SN********************
 *  ktmg1160.p - Cloned from ktmg1110.p
 *  cjw 12/19/16 tkt23434 - New Report for Datamax/Oneil
 *  jmh 02/14/17 tkt 24777 - changed the report to look for serial numbers for
 *       the sales order line specifically and to look for records which match
 *       the ship to address not the sold too.                       
 *  jmh 04/05/17 tkt 25947  massive changes - not commented!  Fixed a prob
 *      where a customer has the same line item invoiced on the same.  The 
 *      program was duplicating the serial numbers for each invoice which 
 *     looked like there had been duplicate serial numbers.  
 * **** We still have a problem in getting only the serial numbers invoiced
 *        on a specific day.  **** NOT ANYMORE - HAPPY BIRTHDAY JO MARIE ****
 * srr 05/18/17 TKT26885 Add sort option 8 for EcoSense report
 * cjt 05/31/17 tkt27151 fixed bug involving sort option 8 no ad_mstr avail.
 * srr 07/24/17 TKT28473 Remove "header lines" when SN displayed. Only display
 *              SN for Packaging types.
 * srr 10/17/17 TKT28001 Support new channels for RMAs
 * jmh 10/18  tkt #39153  added caps to display of all Serial numbers
 * srr 03/29/19 TKT42799 Use invoice # to find SN for single shipment
 * bjr 04/29/19 TKT43801 Add remarks to the end of the worksheet
 * srr 04/24/20 TKT53193 Add KIT SN if found, for new sort option 9
 * srr 04/29/20 TKT53560 used customer option to NOT include price
 * zbd 11/04/20 TKT58518 added site to download file
 * jmh 11/20/20 tkt 58995 removed requiring the ship-to matching wht_custid
 * zbd 12/04/20 Compiled into QAD 2019 - modified .i paths
 * zbd 05/17/21 TKT62597 Added new sort by for HID per Jacob Labeller
 * zbd 05/26/21 TKT62297 Replaced base customer with corp group
 * zbd 08/12/21 TKT67043 Add new sort by for Mirror -- add bol parameters
 * zbd 11/23/21 TKT69899 Update Mirror report to use boxes instead of pallet
 * srr 02/15/22 TKT71794 Add customer option to remove prefix from SN
 * zbd 09/12/22 TKT78432 Create new report for Corning per Elias Montenegro
 * zbd 03/04/23 TKT03145 Add new report opt for Radar that allows 45 char SN
 * zbd 04/10/24 TKT03767 Add paramter for more specific gen code lookup
 * zbd 11/19/24 TKT09112 Add support for Vietnam VT site
 * srr 07/28/25 TKT14121 Add new sort by to display Box and Pallet ID's w/o SN
 * srr 08/25/25 TKT14017 Add new sort by for "Master Carton ID"
 * zbd 01/14/26 TKT18046 Add new ktmachine.p logic to get machine
 * srr 02/09/26 TKT18218 Add Rev and Mfg Date to Gridware (opt 15)
 *                       Also use usr_mstr record for default email address
 * zbd 04/08/26 TKT20306 Upgrade SFTP logic to use ktut1043.i library for
 *                       gridware per Julie B
 * zbd 04/22/26 TKT20157 Require date range OR D/W flag set
 ****************************************************************************/

{us/mf/mfdtitle.i "b "}
{us/kt/ktut1043.i} /*TKT20306*/
 
def var site1 like idh_site format "x(02)".
def var site2 like idh_site label "To" format "x(02)".
def var part1 like idh_part.
def var part2 like idh_part label "To".
def var name like ad_name.
def var pl1  like idh_prodline.
def var pl2  like idh_prodline label "To".
def var base_rpt like ih_curr format "x(4)" initial "Base".
def var incl_memo as logical init yes label "Include Memos".      /*ss060910*/
def var show_desc as logical init no label "Show Part Desc".      /*ss092011*/
def var desc1     like pt_desc1.                                  /*ss092011*/
def var desc2     like pt_desc2.                                  /*ss092011*/
def var sort_by as char format "x(02)" label "Sort By".
def var slspsn1  like sp_addr label "Salesperson".                /*sh 11/96*/
def var slspsn2  like sp_addr label "To".                         /*sh 11/96*/
def var shipto1  like so_ship.
def var sort_by_desc like lngd_translation.
def var cur_cnvrsn like ih_curr.
def var base_price  like idh_price.
def var ext_price as dec format "->,>>>,>>9.99" label "Ext Price".
def var shipname like ad_name.
def var chr2  as character format "x(2)".                         /*sh 11/96*/
def var pos   as character format "x(1)".                         /*sh 11/96*/
def var shiptt as character format "x(1)".                        /*rws 4/02*/
def var ordate1 as date.                                          /*rws 4/02*/
def var ordate2 as date.                                          /*rws 4/02*/
def var date1   as DATE.                                         /*srr120909*/
def var date2   as DATE.                                         /*srr120909*/
def var chdate1 as date.                                          /*csr 2746*/
def var chdate2 as date.                                          /*csr 2746*/
def var emailaddr as char.                                        /*rws 4/02*/
def var attach as char.                                           /*rws 4/02*/
def var emailfrom as char.                                        /*rws 4/02*/
def var emailto as char format "x(50)" label "Email To".          /*rws 4/02*/
def var i as integer.                                             /*rws 4/22*/
def var userpath as char.                                         /*rws 4/22*/
def var filecontents as logical.                                  /*rws 5/7*/
def var wksheet as logical init no label "Worksheet Format?".     /*ss051206*/ 
def var trqty like tr_qty_loc no-undo.                           /*srr033009*/
define new shared variable sochannel as char format "x(1)"       /*srr120909*/
    initial "2" label "Order Type(F2)".                          /*srr120909*/
def var chandesc like code_cmmt.                                 /*srr120909*/
def var kit_exists as logical initial no.                       /*TKT53193*/
def var noprice_rpt as logical initial no.                      /*TKT53560*/
def var noprice_cust as logical initial no.                     /*TKT53560*/
def var hid_ship_date as char.                                  /*TKT62597*/
def var palletid as char.                                       /*TKT62597*/
def var length as char.                                         /*TKT67043*/
def var width  as char.                                         /*TKT67043*/
def var height as char.                                         /*TKT67043*/
def var weight as char.                                         /*TKT67043*/
def var bol1 like ih_bol.                                       /*TKT67043*/
def var bol2 like ih_bol.                                       /*TKT67043*/
def var ztab as char.                                           /*TKT03145*/

def var t_rev like wo_rev.                                      //TKT18218
def var t_mfg_date like wo_rel_date.                            //TKT18218
def var t_wo_lot like wo_lot.                                   //TKT23984

def var sn_prefix_char as char.                                 //TKT71794
def var sn_prefix_length as int.                                //TKT71794

def var mult_inv as char format "x(60)".
def var day_inv_qty like idh_qty_inv.

def var subject as char.                                         /*srr121708*/
subject = "~"Shipments Report from REDACTED - " + string(today) + "~"".

//TKT18218 use user record for email addresses
find first usr_mstr where usr_userid = global_userid no-lock no-error.
if available usr_mstr and usr_mail_address <> ""
    then assign emailto = usr_mail_address
                emailfrom = emailto.
    else assign emailto = trim(global_userid) + "@REDACTED.com"
                emailfrom = emailto.

ztab = chr(09).                                                 /*TKT03145*/
 
/*dsj050608 - add for FTP */
def var do_ftp as logical no-undo.
def var zpostfiles as char extent 100 no-undo.       /*TKT20306*/
def var zpostdir as char no-undo.                    /*TKT20306*/
def var zstagingdir as char format "x(40)".          /*TKT20306*/
def var zarchivedir as char format "x(40)" extent 5. /*TKT20306*/
def var zconfirm as logical.                         /*TKT20306*/
def var ztest as logical.                            /*TKT20306*/  
def var fileExists as logical.                       /*TKT20306*/
def var sftpOutput as char.                          /*TKT20306*/
def var testdir as char.                             /*TKT20306*/
def var zErrorEmail as char.                         /*TKT20306*/
def var zSubject as char.                            /*TKT20306*/
def var zAttach as char.                             /*TKT20306*/

//TKT18046 def var tempfile as char no-undo.
def var dirnm as char format "x(40)" extent 3.
 
def var zinvoice as char format "x(6)".
def var disp_inv_len as int initial 6.

def new shared var zprogram as char format "x(12)" no-undo.
def new shared var dirloc   as char.
def new shared var fname    as char format "x(60)".
def new shared var lProdServer as logical no-undo.       //TKT18046
{us/bbi/gprun.i ""ktmachine.p""}                           //TKT18046
//TKT18046 def new shared var machine  as char format "x(20)" no-undo.

/*TKT03767 Begin Add*/
def var gencode_brparm as char format "x(60)" no-undo.
assign gencode_brparm = "sort_by_so02".

{us/gp/gpbrparm.i &browse=ktlu002.p &parm=c-brparm1 &val="gencode_brparm"} 
/*TKT03767 End Add*/
 
DEF VAR msgtxt AS CHAR.                                         /*cjw061913*/
DEF VAR l_desc AS CHAR FORMAT "x(64)" EXTENT 2.                 /*cjw050812*/
ASSIGN l_desc[1] = "Enter Corp, Cust Range, or Cust Type"
       l_desc[2] = " 'D' for yesterday's or 'W' for last week's shipments.".
 
// TKT18046 Comment Out
// tempfile = "tempfile." + string(random(1,9999)).
// unix silent hostname > value(tempfile).
// machine = "".
// input from value(tempfile).
// import machine.
// input close.
// unix silent rm value(tempfile).
/*dsj050608 - end add */
 
/*srr033009*/
define temp-table tmpRecord
    field tmp_ih_nbr       like ih_hist.ih_nbr
    field tmp_idh_line     like idh_hist.idh_line
    field tmp_idh_site     like idh_site
    field tmp_ih_po        like ih_po
    field tmp_ih_ship_date like ih_ship_date
    field tmp_ih_ord_date  like ih_ord_date
    field tmp_idh_part     like idh_part
    field tmp_idh_custpart like idh_custpart
    field tmp_idh_qty_inv  like idh_qty_inv
    field tmp_idh_price    like idh_price
    field tmp_idh_type     like idh_type
    field tmp_idh_due_date like idh_due_date      /*06/09 jmh*/
    field tmp_idh_per_date like idh_per_date      /*TKT78432*/ 
    field tmp_ih_shipvia   like ih_shipvia
    field tmp_ih_bol       like ih_bol
    field tmp_ih__qad03    like ih__qad03   /*csr 2627*/
    field tmp_ih__dec01    like ih__dec01  /*csr 2627*/
    field tmp_ih_inv_nbr   like ih_inv_nbr
    field tmp_ih_curr      like ih_curr
    field tmp_ih_ex_rate   like ih_ex_rate
    field tmp_ih_cust      like ih_cust
    field tmp_ih_ship      like ih_ship
    field tmp_ih_slspsn    like ih_slspsn[sp_pos]
    field tmp_ih__chr01    like ih__chr01
    field tmp_ih__chr05    like ih__chr05
    field tmp_idh__chr09   like idh__chr09                        /*tkt 9382*/
    FIELD tmp_whs_sn       LIKE whs_sn 
    field tmp_wht_sn       Like wht_sn           /*tkt23434*/
    field tmp_wht_qty      like wht_boxqty       /*TKT67043*/
    field tmp_kit_sn        like whs_sn                             /*TKT53193*/
    field tmp_wht_date_ship as char  
    field tmp_wht_type  like whs_type
    field tmp_whs_type  like whs_type
    field tmp_ih_rmks   like ih_rmks             /* TKT43801 */ 
    field tmp_rev       like wo_rev              //TKT18218
    field tmp_mfg_date  like wo_rel_date         //TKT18218
    field tmp_wo_lot    like wo_lot              //TKT23984
     index idhcust tmp_ih_cust
     index idhship tmp_ih_ship_date
     index idhpart tmp_idh_part.
 
 
/*INCLUDE FILE DEFINES SHARED VARIABLES FOR CALLED PROGRAM ktad1021.p*/
/*AS WELL AS VARIABLES FOR THE BASE CUSTOMER/CUSTOMER/CUSTOMER TYPE(F2)*/
/*FUNCTIONALITY AND SALESPERSON:_____ POS: _____ EXTRACTION*/
{us/kt/ktso1184.i}
 
userpath = "/users/" + trim(global_userid) + "/".

form
  /*"Enter either a Base Customer, Customer Range, or a Customer Type" at 12*/
    /*l_desc[1] no-label AT 12 SKIP*/ /*TKT67043*/              /*cjw050812*/
    sel_group colon 18                                          /*TKT62297*/
    l_desc[1] no-label AT 41 format "x(38)" SKIP                /*TKT67043*/
    cust1     colon 18
    cust2     colon 50 label "To"
    zregion   colon 18 label "Customer Region"
    zregion1  colon 50 label "To"
    cust_type colon 18
    cust_type_desc colon 22 no-label
/*srr120909*    skip(1) */
    site1     colon 18
    site2     colon 50
    part1     colon 18
    part2     colon 50
    pl1       colon 18
    pl2       colon 50
    slspsn1   colon 18                                           /*sh 11/96*/
    slspsn2   colon 50                                           /*sh 11/96*/
    sp_pos    colon 66
    shipto1   colon 18
    base_rpt  colon 50
    shiptt    colon 18 label "Report Timetable"
    /*" 'D' for yesterday's or 'W' for last week's shipments."*/
    l_desc[2] no-label FORMAT "x(53)"                           /*cjw050812*/
    date1     colon 18 label "-OR- Invoice Date"                 /*ss042610*/
    date2     colon 50 label "To"                                /*srr120909*/
    bol1      colon 18 label "BOL"                               /*TKT67043*/
    bol2      colon 50 label "To"                                /*TKT67043*/
    incl_memo colon 18                                           /*ss060910*/
    sort_by   colon 18
    sort_by_desc colon 25 no-label
    sochannel colon 18                                           /*srr120909*/
    chandesc  colon 25 no-label                                  /*srr120909*/
    emailto   colon 18
    wksheet   colon 18                                           /*ss051206*/
    show_desc colon 50                                           /*ss092011*/
 /* skip */                                                      /*ss060910*/
with frame a width 80 side-labels.
 
DISPLAY l_desc WITH FRAME a.
 
find first gl_ctrl where gl_ctrl.gl_domain = global_domain no-lock.
 
repeat:
    kit_exists = no.                                            /*TKT53193*/
    for each tmpRecord: delete tmpRecord. end.                 /*srr033009*/
 
    if date1 = low_date then date1 = ?.     /*srr120909*/
    if date2 = hi_date  then date2 = ?.     /*srr120909*/
    if shiptt <> "" then assign date1 = ?   /*srr120909*/
                                date2 = ?.  /*srr120909*/
    if site2 = hi_char then site2 = "".
    if part2 = hi_char then part2 = "".
    if pl2   = hi_char then pl2   = "".
    if zregion1 = hi_char then zregion1 = "".
    if cust2 = hi_char then cust2 = "".                         /*dsj022801*/
    if slspsn2 = hi_char then slspsn2 = "".                     /*ss 041601*/
    if bol2 = hi_char then bol2 = "".                           /*TKT67043*/
 
    filecontents = no.                                             /*ss051106*/
 
    update
        sel_group validate(sel_group = "" or can-find(corporategroup /*TKT62297*/
            where corporategroupcode = sel_group),                   /*TKT62297*/
            " Must enter valid Corp Group.  Press F2 ")              /*TKT62297*/
        cust1 validate(cust1 = "" or can-find(cm_mstr
            where cm_domain = global_domain and cm_addr = cust1),
            " Must enter valid Customer.  Press F2 ")
        cust2 validate(cust2 = "" or can-find(cm_mstr
            where cm_domain = global_domain and cm_addr = cust2),
            " Must enter valid Customer.  Press F2 ")
        zregion zregion1 cust_type
        site1 site2 part1 part2 pl1 pl2
        slspsn1 slspsn2                                           /*sh 11/96*/
        sp_pos shipto1 base_rpt
                                          /*ss051106 - Added validates below */
        shiptt validate(shiptt = "D" or shiptt = "W" or shiptt = "",
            " Must enter a valid Report Timetable")
        date1 date2                                             /*srr120909*/
        bol1 bol2                                                 /*TKT67043*/
        incl_memo                                                 /*ss060910*/
        sort_by validate(can-find(first code_mstr
            where code_domain = global_domain
              and code_fldname = "sort_by_so02" and code_value = sort_by),
            " Must enter valid sort-by. Press F2 ")
          sochannel                                             /*srr120909*/
        emailto validate(emailto <> "",
            " You must enter an email address.")
        wksheet                                                   /*ss051206*/
        show_desc                                                 /*ss092011*/
    with frame a.

    if sel_group <> "" and cust_type <> "" then do:               /*TKT62297*/
        msgtxt = " Enter EITHER a Corp Group, OR Customer Range," /*TKT62297*/
                 + "OR a Customer Type ".
        {us/bbi/pxmsg.i &MSGTEXT=msgtxt &ERRORLEVEL=1}
        next-prompt sel_group with frame a.                       /*TKT62297*/
        undo, retry.
    end.
 
    if sel_group = "" and cust_type = "" then                     /*TKT62297*/
        msgtxt = " Report will run for specified customer range ".
        {us/bbi/pxmsg.i &MSGTEXT=msgtxt &ERRORLEVEL=1}
 
    if sel_group <> "" then do:
        msgtxt = "Report will run for selected Corp Group only".  /*TKT62297*/
        {us/bbi/pxmsg.i &MSGTEXT=msgtxt &ERRORLEVEL=1}
                                                           /*srr121708*/
        subject = "~"" + CAPS(trim(sel_group))
                  + " Shipments Report from REDACTEDEMS - "
                  + string(today) + "~"".
    end.
    if cust1 <> "" then                                    /*srr121708*/
        subject = "~"" + CAPS(trim(cust1))
                  + " Shipments Report from REDACTEDEMS - "
                  + string(today) + "~"".
 
                                                      /*srr120909 - Begin add */
   if shiptt <> "" and (date1 <> ? or date2 <> ?) then do:    
      msgtxt = "You cannot enter both a timetable and a date range.".
      {us/bbi/pxmsg.i &MSGTEXT=msgtxt &ERRORLEVEL=1}
      undo, retry.
   end.                                               /*srr120909 - End add */

   //TKT20157 Begin Add
    if shiptt = "" and (date1 = ? or date2 = ? or (date2 - date1 > 90)) then do: 
        msgtxt = "Must enter either a Report Timetable OR a valid Invoice Date range of 90 days or less".
        {us/bbi/pxmsg.i &MSGTEXT=msgtxt &ERRORLEVEL=1}
        undo, retry.
    end.                                                                      
    //TKT20157 End Add
 
    /* Begin rws 4/02 add*/
    if caps(shiptt) = "D" then do:
        /* if string(time,"hh:mm:ss") > "15:00:00" then do:     **cjw050412*/
        FIND FIRST usrw_wkfl WHERE usrw_domain = global_domain
                       AND usrw_key1 = "control_key"
                       AND usrw_key2 = "BATCH_DATE_CHANGE" 
                       NO-LOCK NO-ERROR.                        /*cjw050412*/
        IF AVAILABLE usrw_wkfl THEN DO:                         /*cjw062212*/             
          IF STRING(TIME,"hh:mm:ss") > usrw_charfld[1] THEN DO: /*cjw050412*/
              ordate1 = today.   
              ordate2 = ordate1. 
          end.
          else do:
              ordate1 = today - 1.
              ordate2 = ordate1.
          END.
        END. /*if available*/                                   /*cjw062212*/
    end.
    else if caps(shiptt) = "W" then do:
        ordate1 = today - (weekday(today) + 7).
        ordate2 = today.
    end.
    /* End rws 4/02 add */
    else do:                                                 /*srr120909*/
        if date1 = ? then date1 = low_date.                  /*srr120909*/
        if date2 = ? then date2 = hi_date.                   /*srr120909*/
        ordate1 = date1.                                     /*srr120909*/
        ordate2 = date2.                                     /*srr120909*/
    end.                                                     /*srr120909*/

/*csr 2746 - Begin add */
    if caps(shiptt) = "D" 
    then assign chdate1 = today
                chdate2 = today.
    else assign chdate1 = ordate1
                chdate2 = ordate2.
/*csr 2746 - End add */            
 
/*dsj051101 - Begin added section */
    if cust_type <> "" then do:
        find first code_mstr where code_domain = global_domain
                               and code_fldname = "cust_type"
                               and code_value = cust_type no-lock no-error.
        if not available code_mstr then do:
            msgtxt = " Must enter valid customer type.  Press F2 ".
            {us/bbi/pxmsg.i &MSGTEXT=msgtxt &ERRORLEVEL=1}
            next-prompt cust_type with frame a.
            undo, retry.
        end.
        cust_type_desc = code_cmmt.
        display cust_type_desc with frame a.
        msgtxt = "Report will run for selected Customer Type only".
        {us/bbi/pxmsg.i &MSGTEXT=msgtxt &ERRORLEVEL=1}
    end.
/*dsj051101 - End added section */
 
    find first code_mstr where code_mstr.code_domain = global_domain
                           and code_fldname = "sort_by_so02"    /*dsj051101*/
                           and code_value = sort_by             /*dsj051101*/
                         no-lock no-error.                      /*dsj051101*/
 
    sort_by_desc = code_cmmt. /*lngd_translation.*/             /*dsj051101*/
    display sort_by_desc with frame a.
 
/*dsj050608 - add for FTP bit */
    find first usrw_wkfl where usrw_wkfl.usrw_domain = global_domain
                           and usrw_key1 = "distlist"
                           and usrw_key2 = emailto
                         no-lock no-error.
    if available usrw_wkfl and usrw_user1 = "FTP" then do:
        assign do_ftp = yes
               zprogram = usrw_charfld[1]
               zpostdir = usrw_charfld[2]             /*TKT20306*/
               zstagingdir = usrw_charfld[3]          /*TKT20306*/
               zarchivedir[1] = usrw_charfld[4]       /*TKT20306*/
               zconfirm = (usrw_charfld[5] = "TRUE")  /*TKT20306*/
               ztest = (lProdServer = false)          /*TKT20306*/
               zErrorEmail = usrw_charfld[6].         /*TKT20306*/

        find first code_mstr where code_domain = global_domain
                               and code_fldname = "sn_program"
                               and code_value = zprogram
                             no-lock no-error.
        if not available code_mstr or zprogram = "" then do:
            msgtxt = "Distribution list "
                    + emailto 
                    + " is not set up correctly for FTP".
            {us/bbi/pxmsg.i &MSGTEXT=msgtxt &ERRORLEVEL=1}
        end.
        else do:
            assign dirloc = "shipments/"
                   dirnm[1] = "/dropbox/" + lc(code_cmmt)
                            + "/fromkt/" + dirloc
                   dirnm[2] = "/users/"   + lc(code_cmmt)
                            + "/fromkt/" + dirloc
                   testdir  = "/users/" + global_userid + "/". /*TKT20306*/
                    
                    /*TKT20306 Comment Out
                    * dirnm[3] = "/workspace/" + lc(code_cmmt)
                    *        + "/fromkt/" + dirloc*/
        
                 /*TKT20306 Replace Below          
                 *fname = lc(code_cmmt) + "ship"
                 *        + substr(string(today),7,2)
                 *        + substr(string(today),1,2)
                 *        + substr(string(today),4,2).*/

                 /*TKT20306 Begin Add*/
                 fname = "ShipRptSn"
                         + replace(string(today), "/", "")
                         + replace(string(time, "hh:mm:ss"), ":", "").
                 /*TKT20306 End Add*/

            if lProdServer = true then do i = 1 to 2: //TKT18046 machine = "rh7qad2019" //TKT20306 reduced from 3 to 2 directories
                os-create-dir value(dirnm[i]).
            end.
        end.
    end.
/*dsj050608 end add */
 
/*srr120909 - add*/
    find first code_mstr where code_domain  = global_domain 
                           and code_fldname = "sochannel"
                           and code_value   = sochannel no-lock no-error.
    IF NOT AVAILABLE code_mstr THEN DO:                       
        msgtxt = " Enter order types desired. Press F2 for help. ".
        {us/bbi/pxmsg.i &MSGTEXT=msgtxt &ERRORLEVEL=1}
        NEXT-PROMPT sochannel WITH FRAME A.
        UNDO, RETRY.
    END.
    chandesc = code_cmmt.
    display chandesc with frame a.
 
    if shipto1 <> "" and sort_by <> "2" then
        msgtxt = "Warning....Ship-to address only looked at for Sort-by option '2'".
        {us/bbi/pxmsg.i &MSGTEXT=msgtxt &ERRORLEVEL=1}
 
    bcdparm = "".
    {us/mf/mfquoter.i sel_group}                                    /*TKT62297*/
    {us/mf/mfquoter.i cust1}
    {us/mf/mfquoter.i cust2}
    {us/mf/mfquoter.i zregion}
    {us/mf/mfquoter.i zregion1}
    {us/mf/mfquoter.i cust_type}
    {us/mf/mfquoter.i site1}
    {us/mf/mfquoter.i site2}
    {us/mf/mfquoter.i part1}
    {us/mf/mfquoter.i part2}
    {us/mf/mfquoter.i pl1}
    {us/mf/mfquoter.i pl2}
    {us/mf/mfquoter.i slspsn1}                                          /*sh 11/96*/
    {us/mf/mfquoter.i slspsn2}                                          /*sh 11/96*/
    {us/mf/mfquoter.i sp_pos}
    {us/mf/mfquoter.i shipto1}
    {us/mf/mfquoter.i base_rpt}
    {us/mf/mfquoter.i shiptt}
    {us/mf/mfquoter.i date1}                                          /*srr120909*/
    {us/mf/mfquoter.i date2}                                          /*srr120909*/
    {us/mf/mfquoter.i bol1}                                            /*TKT67043*/
    {us/mf/mfquoter.i bol2}                                            /*TKT67043*/
    {us/mf/mfquoter.i incl_memo}                                       /*ss060910*/
    {us/mf/mfquoter.i sort_by}
    {us/mf/mfquoter.i sochannel}                                       /*srr120909*/
    {us/mf/mfquoter.i emailto}
    {us/mf/mfquoter.i wksheet}                                         /*ss051206*/
    {us/mf/mfquoter.i show_desc}                                       /*ss092011*/
 
    /*ss 041601 -- Moved following call below mfquoter*/
    /* To edit slspsn1, slspsn2, and sp_pos screen entries.    ******sh 11/96*/
    {us/kt/ktso1200.i}                                                   /*sh 11/96*/
 
    if site2 = "" then site2 = hi_char.
    if part2 = "" then part2 = hi_char.
    if pl2   = "" then pl2   = hi_char.
    if zregion1 = "" then zregion1 = hi_char.
    if cust2 = "" then cust2 = hi_char.                         /*dsj022801*/
    if slspsn2 = "" then slspsn2 = hi_char.                     /*dsj042701*/
    if bol2 = "" then bol2 = hi_char.                           /*TKT67043*/

    {us/mf/mfselbpr.i "printer" 132}
 
    /*INCLUDE FILE CREATES CUSTOMER TEMP-TABLE*/
    /*{ktso1186.i}*/
    {us/kt/ktso1194.i}

    /*TKT53560 - begin add*/
    assign noprice_rpt = no
           noprice_cust = no.
    for each t_cust no-lock:
        {us/kt/ktcustoptl.i "t_addr" "'Shipment_rpt_remove_price'" "noprice_cust"} 
        /* if even one customer has option set, do not show price on report */
        if noprice_cust = yes then noprice_rpt = yes.
    end.
    /*TKT53560 - end add*/

    /*rws 4/02*/
    if wksheet 
        and not sort_by = "13"                               /*TKT03145*/
    then
        assign attach = userpath + "ShipRpt"
                      + replace(string(today), "/", "") + ".csv"
               fname  = fname + ".csv".                         /*dsj050608*/
    else
        assign attach = userpath + "ShipRpt"
                      + replace(string(today), "/", "") + ".txt"
               fname  = fname + ".txt".                         /*dsj050608*/
    
    if do_ftp = no then                                         /*dsj050608*/
        output to value(attach + ".temp").                       /*rws 4/02*/
/*dsj050608 - add */
    else do:
        if lProdServer = true then //TKT18046 machine = "rh7qad2019" 
             output to value(dirnm[2] + fname + ".temp").
        else output to value(testdir + fname + ".temp"). /*TKT20306 added testdir*/
    end.
/*dsj050608 - end */
 
/*srr033009 - begin add*/
   /* Add Consigned Shipments (uninvoiced) to Table */
   for each t_cust,
       each tr_hist no-lock where tr_domain = global_domain
                    and tr_addr = t_addr
/*csr 2746*/        and ((tr_site <> "CH" and 
                          tr_site <> "VT" and                                   //TKT09112
/*csr 2746*/              tr_effdate >= ordate1 and tr_effdate <= ordate2) or
/*csr 2746*/             ((tr_site = "CH" or tr_site = "VT") and                //TKT09112 add 'or tr_site = "VT"'
/*tkt 7976*/              tr_effdate >= chdate1 and tr_effdate <= chdate2))
                    and tr_part >= part1 and tr_part <= part2
                    and tr_site >= site1 and tr_site <= site2
                    and tr_slspsn[sp_pos] >= slspsn1
                    and tr_slspsn[sp_pos] <= slspsn2
                    and tr_type = "CN-SHIP"
                    and (tr_ship_type <> "M" or incl_memo)        /*ss060910*/
                    and (base_rpt = "Base" or tr_curr = base_rpt), 
       each pt_mstr no-lock  where pt_mstr.pt_domain = global_domain
                    and pt_part = tr_part
                    and pt_prod_line >= pl1 and pt_prod_line <= pl2,
       first cm_mstr no-lock where cm_domain = global_domain
                     and cm_addr = tr_addr
                     and cm_type ne "INTR" and cm_type ne "SUBC":
 
          run get_consign_qty (output trqty).
          Run createTrRecord.
 
    end. /* each pt_mstr*/
/*srr033009 - end add*/
 
/*srr033009 - begin add*/
    for each t_cust,
        each ih_hist no-lock
              where ih_hist.ih_domain = global_domain 
                and ih_cust = t_addr 
                and ih_bol >= bol1                               /*TKT67043*/
                and ih_bol <= bol2                               /*TKT67043*/
                and ih_slspsn[sp_pos] >= slspsn1                 /*sh 11/96*/
                and ih_slspsn[sp_pos] <= slspsn2                 /*sh 11/96*/
                and (base_rpt = "Base" or ih_curr = base_rpt) 
                and ih_channel <> "ADJ"                          /*ss040411*/
/*TKT28001*
*                and ((sochannel = "1" and ih_channel = "RMA")   /*srr120909*/
*                  or (sochannel = "2" and ih_channel <> "RMA")  /*srr120909*/ */
                and ((sochannel = "1" and ih_channel begins "RMA")   /*TKT28001*/
               or (sochannel = "2" and not ih_channel begins "RMA")  /*TKT28001*/
                  or (sochannel = "3"))                         /*srr120909*/
                and not(can-find(first cncu_mstr
                        where cncu_mstr.cncu_domain = global_domain
                          and cncu_invoice = ih_inv_nbr)),      /*srr033009*/
        each idh_hist no-lock
              where idh_hist.idh_domain = global_domain
                and idh_inv_nbr = ih_inv_nbr
/*tkt 20362*/   and ((idh_site <> "CH" and 
                      idh_site <> "VT" and                                   //TKT09112 
/*tkt 20362*/         ih_inv_date >= ordate1 and ih_inv_date <= ordate2) or
/*tkt 20362*/        ((idh_site = "CH" or idh_site = "VT") and               //TKT09112 add 'or idh_site = "VT")'
/*tkt 20362*/         ih_inv_date >= chdate1 and ih_inv_date <= chdate2))
                and (idh_site >= site1 and idh_site <= site2) and
                    (idh_part >= part1 and idh_part <= part2) and
                    (idh_prodline >= pl1 and idh_prodline <= pl2)
                and (idh_type <> "M" or incl_memo)               /*ss060910*/
               break by idh_nbr by idh_line:
        
     /*srr050409 - also skip invoices for consignment usages which are undone*/
        find first cncix_mstr where cncix_domain  = global_domain
                                and cncix_so_nbr  = ih_nbr no-lock no-error.
        if available cncix_mstr then next.
 
        mult_inv = mult_inv + ih_inv_nbr + ";".
        day_inv_qty = day_inv_qty + idh_qty_inv.
        
        if last-of(idh_line) then do: 
            Run createInvRecord(yes).                           /*TKT42799*/
            mult_inv = "".
            day_inv_qty = 0.
       end. 
       else run createInvRecord(no).                            /*TKT42799*/
    end. /* each t_cust */
/*srr033009 - end add*/
 
/*TKT53193 moved after creating tmpRecords for optional KIT display*/
    if wksheet and sort_by = "8" then do:                           /*TKT26885*/
        export delimiter ","
            "Purchase Order #"
            "Customer Item" 
            "Serial Number" "Ship Date".
    end.
           
    else if wksheet then do:                          /*ss051206 - Begin add */
        /*TKT62597 Begin Add*/
        if sort_by = "10" then do:
            put unformatted
                "HidDeviceUniqueId" ','
                "PartNumber" ','
                "PurchaseOrder" ','
                "BoxId" ','
                "PaletteId" ','
                "AdvancedShippingNotice" ','
                "ShippingDate" ','
                "TrackingNumber"
                skip.
        end.
        /*TKT67043 Begin Add*/
        else if sort_by = "11" then do:
            put unformatted 
                "Ref #"                              ','
                "PO #"                               ','
                "ShipCarrier"                        ','
                "Notes"                              ','
                "SKU"                                ','
                "SKU Quantity"                       ','
                "Lot#"                               ','
                "Serial#"                            ','
                "Expiration Date"                    ','
                "LocationField1"                     ','
                "LocationField2"                     ','
                "LocationField3"                     ','
                "LocationField4"                     ','
                "Cost"                               ','
                "Var UOM Avg"                        ','
                "Receipt Advice Number"              ','
                "TrackingNumber"                     ','
                "SupplierCompnayName"                ','
                "MULabel"                            ','
                "MUType"                             ','
                "MULength"                           ','
                "MUWidth"                            ','
                "MUHeight"                           ','
                "MUWeight"                           ','
                "Receiver Transaction Saved Element" ','
                "CreateMultipleMUs"
                skip.
        end.
        /*TKT67043 End Add*/        
        
        /*TKT78432 Begin Add*/
        else if sort_by = "12" then do:
            put unformatted
                "Purchase Order"        ","
                "Line"                  ","
                "Order"                 ","
                "Shipped Date"          ","
                "Original Commit"       ","
                "Order Date"            ","
                "Item Number"           ","
                "Description"           ","
                "Qty Ordered"           ","
                "Ship Via"              ","
                "Bill of Lading"         
                skip.
        end.
        /*TKT78432 End Add*/

        /*TKT03145 begin add*/
        else if sort_by = "13" then do:
            put unformatted
                "Customer No" ztab
                "Customer Name" ztab
                "Ship To" ztab
                "SO #" ztab
                "Purchase Order No" ztab
                "Ship Date" ztab
                "Level" ztab
                "Item No" ztab
                "Sub Item No" ztab
                "Sub Serial No" ztab
                "Customer Item No" ztab
                "Description 1" ztab
                "Description 2" ztab
                "Qty Shipped" ztab
                "Unit Price" ztab
                "T" ztab
                "Ship Via" ztab
                "BOL" ztab
                "Invoice No" ztab
                "LN" ztab
                "Site" ztab
                "Serial Number" ztab
                "SN Type" ztab
                "SN Ship Date" ztab
                "Pallet ID" 
                skip.
        end.
        /*TKT03145 end add*/

        else do:                                        /*TKT62597 End Add*/
            export delimiter ","
                "Customer" "Name" "Ship To" "SO #" "Purch Order" "Ship Date"
                "Item Number" "Customer Item" 
                "Description 1" when show_desc = yes                  /*ss092011*/
                "Description 2" when show_desc = yes                  /*ss092011*/
                "Qty Inv" 
                "Unit Price"    when noprice_rpt = no                 /*TKT53560*/
                "T" "Ship Via" "BOL" "Invoice#" "LN"   /*csr 2581*/
                "Site"                                                /*TKT58518*/
                "Boxes" "Pallets" /*csr 2627*/
                "Release#"                                            /*tkt 9382*/
                "Serial Number" "SN Type" 
                "S/N Ship Date"  /*tkt 24777*/
                "Master Carton ID" when sort_by = "15"              //TKT14017
                "Pallet ID" when sort_by <> "15"                    //TKT14017
                "Customer Serial Number" when kit_exists = yes      /*TKT53193*/
                "Remarks" when sort_by = "7" or sort_by = "9"       /*TKT43801*/
                            or sort_by = "15"                       //TKT14017
                "Version" when sort_by = "15"                       //TKT18218
                "Mfg Date" when sort_by = "15"                      //TKT18218
                "Work Order ID" when sort_by = "15".                //TK23984
        end. /*TKT62597*/
    end.
    else do:                                          /*ss051206 - End add */
        {us/bbi/mfphead.i}
    end.

    if sort_by = "1"
    or sort_by = "14"                                   //TKT14121
    then do:
        for each tmpRecord no-lock
           break by tmp_ih_cust by tmp_ih_ship_date by tmp_idh_part 
              BY tmp_whs_sn
               with frame b width 132 no-box:
 
           filecontents = yes.                                   /*rws 5/7*/
 
           assign base_price = tmp_idh_price
                  cur_cnvrsn = "".
 
           if base_curr <> tmp_ih_curr and base_rpt = "Base" then
              assign base_price = base_price / tmp_ih_ex_rate
                     cur_cnvrsn = tmp_ih_curr.
 
           find cm_mstr where cm_mstr.cm_domain = global_domain
                          and cm_addr = tmp_ih_cust 
                          no-lock no-error.
 
           shipname = "".
           find ad_mstr where ad_mstr.ad_domain = global_domain
                          and ad_addr = tmp_ih_ship no-lock no-error.
           if available ad_mstr then shipname = ad_sort.
           IF AVAILABLE cm_mstr THEN DO:
            IF shipname = ""     THEN shipname = cm_sort.
           END.
 
           desc1 = "".                                /*ss092011 - Begin add */
           desc2 = "".
           find first pt_mstr where pt_domain = global_domain
                               and pt_part = tmp_idh_part no-lock no-error.
           if available pt_mstr
              then assign desc1 = pt_desc1
                          desc2 = pt_desc2.           /*ss092011 - End add */
 
                                    /*csr 2490 */
         {us/bbi/gprun.i ""ktinvformat.p""
               "(input tmp_ih_inv_nbr, input disp_inv_len, output zinvoice)"}


           if wksheet then do:                        /*ss051206 - Begin add */
              export delimiter ","
                 tmp_ih_cust 
                 cm_sort WHEN AVAILABLE cm_mstr
                 "" WHEN NOT AVAILABLE cm_mstr
                 shipname 
                 tmp_ih_nbr 
                 tmp_ih_po 
                 tmp_ih_ship_date 
                 tmp_idh_part 
                 tmp_idh_custpart
                 desc1 when show_desc = yes                       /*ss092011*/
                 desc2 when show_desc = yes                       /*ss092011*/
                 tmp_idh_qty_inv  WHEN tmp_ih_po <> ""
                 ""               WHEN tmp_ih_po =  ""
                 base_price       WHEN tmp_ih_po <> "" 
                                    and noprice_rpt = no          /*TKT53560*/
                 ""               WHEN tmp_ih_po =  ""
                                    and noprice_rpt = no          /*TKT53560*/
                 caps(tmp_idh_type) 
                 tmp_ih_shipvia 
                 tmp_ih_bol
                 tmp_ih_inv_nbr
                 tmp_idh_line  WHEN tmp_ih_po <> ""       /*csr 2581*/ /*LN*/
                 ""            WHEN tmp_ih_po =  ""
                 tmp_idh_site                             /*TKT58518*/
                 tmp_ih__qad03 WHEN tmp_ih_po <> "" /*csr 2627*/     /*Boxes*/
                 ""            WHEN tmp_ih_po =  ""
                 tmp_ih__dec01 WHEN tmp_ih_po <> ""   /*csr 2627*/ /*Pallets*/
                 ""            WHEN tmp_ih_po =  ""
                 tmp_idh__chr09 /*tkt 9382*/       /*Release#*/
                 caps(tmp_whs_sn)   /*#39153*/ /*tkt23434*/ /*Serial Number*/
                 tmp_whs_type
                 tmp_wht_date_ship
                 tmp_wht_sn.
           end.
           else do:                                   /*ss051206 - End add */
              form tmp_ih_nbr /*at 2*/ label "SO #"               /*dsj030608*/
                   tmp_idh_line                                   /*dsj053106*/
                   tmp_idh_site     label "Si" format "x(02)"     /*dsj030608*/
                   tmp_ih_po        label "Purch Order" format "x(12)"
                   tmp_ih_ship_date
                   tmp_idh_part     format "x(16)"
                   tmp_idh_custpart format "x(14)"
                   tmp_idh_qty_inv  column-label "Qty!Shipped" format "->>>,>>9"
                   base_price   column-label "Unit!Price"
                                       format "->>,>>9.99"
                   tmp_idh_type
                   tmp_ih_shipvia   format "x(12)"
                   tmp_ih_bol
                   zinvoice /*tmp_ih_inv_nbr csr 2490 */
                        column-label "Inv #" format "x(06)"
               with frame b.
 
              if first-of(tmp_ih_cust) then do:
                 if page-size - line-counter < 5 then page.
                 display with frame b.
                 if cur_cnvrsn = "" then
                    put "Customer: "
                         tmp_ih_cust
                        " "
                         cm_sort.
                 else
                    put "Customer: "
                         tmp_ih_cust
                        " "
                         cm_sort
                        "    "
                        "Currency: "
                         cur_cnvrsn.
 
                 find sp_mstr where sp_mstr.sp_domain = global_domain
                                and sp_addr = tmp_ih_slspsn  /*sh 11/96*/
                              no-lock no-error.
                 if available sp_mstr then do:                   /*sh 11/96*/
                    chr2 = string(sp_pos).                       /*sh 11/96*/
                    pos = chr2.                                  /*sh 11/96*/
                    put " Salesperson("
                            pos                                  /*sh 11/96*/
                           ") is "
                            sp_addr                              /*sh 11/96*/
                           " - "
                            sp_sort.
                 end.
                 put skip(1).
              end.
 
              accumulate tmp_idh_qty_inv (total by tmp_ih_cust).
           /* accumulate base_price (total by ih_cust). */        /*ss051206*/
 
              if page-size - line-counter < 5 then page.

                                                     /*csr 2490 */
         {us/bbi/gprun.i ""ktinvformat.p""
               "(input tmp_ih_inv_nbr, input disp_inv_len, output zinvoice)"}

 
              display tmp_ih_nbr
                      tmp_idh_line                                /*dsj053106*/
                      tmp_idh_site                                /*dsj030608*/
                      tmp_ih_po
                      tmp_ih_ship_date
                      tmp_idh_part
                      tmp_idh_custpart
                      tmp_idh_qty_inv
                      base_price
                      tmp_idh_type
                      tmp_ih_shipvia
                      tmp_ih_bol
                      zinvoice   /* csr 2490 tmp_ih_inv_nbr  */
                      with frame b.
 
           /* Begin added section 05/02/01 */
              find first so_mstr where so_mstr.so_domain = global_domain
                                   and so_nbr = tmp_ih_nbr no-lock no-error.
              if available so_mstr then do:
                 if so_channel <> "Standard" and so_channel <> "" then do:
                    find first code_mstr where code_domain  = global_domain
                                           and code_fldname = "so_channel"
                                           and code_value   = so_channel
                                         no-lock no-error.
                    if available code_mstr then do:
                       put unformatted
                           so_channel at 2
                           code_cmmt at 11 /*skip*/.              /*tkt 9382*/
                    end.
                 end.
              end.
           /* End added section   05/02/01 */
 
              /*tkt 9382 - Added */
              if tmp_idh__chr09 <> "" then                         
                 put unformatted "Release# " at 47               
                     tmp_idh__chr09 at 56 format "x(15)" skip.
              else put skip.       
              
              if last-of(tmp_ih_cust) then do:
                 if page-size - line-counter < 5 then page.
                 underline tmp_idh_qty_inv
/*ss051206*/            /* base_price */
                           with frame b.
                 display  "Customer Total:" @ tmp_idh_custpart
                           accum total by tmp_ih_cust (tmp_idh_qty_inv)
                                 @ tmp_idh_qty_inv
                               /*format "->,>>>,>>9"*/          /*dsj030608*/
                                 format "->>>,>>9"              /*dsj030608*/
/*ss051206*/            /* accum total by ih_cust (base_price)
/*sh 02-98*/                     @ base_price format "->,>>>,>>9.99" */
                           with frame b.
              end.
 
              if last(tmp_ih_cust) then do:
                 if page-size - line-counter < 5 then page.
                 underline tmp_idh_qty_inv
/*ss051206*/            /* base_price */
                           with frame b.
                 display  "  Report Total:" @ tmp_idh_custpart
                           accum total tmp_idh_qty_inv @ tmp_idh_qty_inv
                               /*format "->,>>>,>>9"*/          /*dsj030608*/
                                 format "->>>,>>9"              /*dsj030608*/
/*ss051206*/            /* accum total base_price @ base_price
/*sh 02-98*/                                       format "->,>>>,>>9.99" */
                           with frame b.
              end.
           end. /* else not wksheet */                            /*ss051206*/
       end. /* for each t_cust */
    end. /* if sort_by = 1 */
 
    if sort_by = "2" then
    do:
        for each tmpRecord no-lock
           break by tmp_ih_ship by tmp_ih_ship_date by tmp_idh_part 
                 BY tmp_whs_sn
              with frame c width 132 no-box:
 
           filecontents = yes.                                    /*ss051106*/
           assign base_price = tmp_idh_price
                  cur_cnvrsn = "".
 
           if base_curr <> tmp_ih_curr and base_rpt = "Base" then
              assign base_price = base_price / tmp_ih_ex_rate
                     cur_cnvrsn = tmp_ih_curr.
           base_price = tmp_idh_price.
 
           find cm_mstr where cm_mstr.cm_domain = global_domain
                          and cm_addr = tmp_ih_cust no-lock no-error.
 
           shipname = "".
           find ad_mstr where ad_mstr.ad_domain = global_domain
                          and ad_addr = tmp_ih_ship no-lock no-error.
           if available ad_mstr then shipname = ad_sort.
           IF AVAILABLE cm_mstr THEN DO:
            IF shipname = ""     THEN shipname = cm_sort.
           END.
 
           desc1 = "".                                /*ss092011 - Begin add */
           desc2 = "".
           find first pt_mstr where pt_domain = global_domain
                               and pt_part = tmp_idh_part no-lock no-error.
           if available pt_mstr
              then assign desc1 = pt_desc1
                          desc2 = pt_desc2.           /*ss092011 - End add */
 

          {us/bbi/gprun.i ""ktinvformat.p""
               "(input tmp_ih_inv_nbr, input disp_inv_len, output zinvoice)"}

           if wksheet then do:                        /*ss051206 - Begin add */
              export delimiter ","
                 tmp_ih_cust cm_sort shipname tmp_ih_nbr tmp_ih_po 
                 tmp_ih_ship_date
                 tmp_idh_part tmp_idh_custpart 
                 desc1 when show_desc = yes                       /*ss092011*/
                 desc2 when show_desc = yes                       /*ss092011*/
                 tmp_idh_qty_inv  WHEN tmp_ih_po <> ""
                 ""               WHEN tmp_ih_po =  ""
                 base_price       WHEN tmp_ih_po <> "" 
                                    and noprice_rpt = no          /*TKT53560*/
                 ""               WHEN tmp_ih_po =  ""
                                    and noprice_rpt = no          /*TKT53560*/
                 caps(tmp_idh_type) tmp_ih_shipvia tmp_ih_bol 
                 tmp_ih_inv_nbr
                 tmp_idh_line  WHEN tmp_ih_po <> ""       /*csr 2581*/ /*LN*/
                 ""            WHEN tmp_ih_po =  ""
                 tmp_idh_site                             /*TKT58518*/
                 tmp_ih__qad03 WHEN tmp_ih_po <> "" /*csr 2627*/     /*Boxes*/
                 ""            WHEN tmp_ih_po =  ""
                 tmp_ih__dec01 WHEN tmp_ih_po <> ""   /*csr 2627*/ /*Pallets*/
                 ""            WHEN tmp_ih_po =  ""
                 tmp_idh__chr09  /*tkt 9382*/
                 caps(tmp_whs_sn)  /*#39153*/ /*tkt23434*/ /*Serial Number*/
                 tmp_whs_type
                 tmp_wht_date_ship
                 tmp_wht_sn.
 
             end.
           else do:                                   /*ss051206 - End add */
              form tmp_ih_nbr /*at 2*/ label "SO #"               /*dsj030608*/
                   tmp_idh_site     label "Si" format "x(02)"     /*dsj030608*/
                   tmp_ih_po        label "Purch Order" format "x(12)"
                   tmp_ih_ship_date
                   tmp_idh_part     format "x(16)"
                   tmp_idh_custpart format "x(14)"
                   tmp_idh_qty_inv  column-label "Qty!Shipped" format "->>>,>>9"
                   base_price   column-label "Unit!Price"
                                       format "->>,>>9.99"
                   tmp_idh_type
                   tmp_ih_shipvia   format "x(13)"
                   tmp_ih_bol
                   zinvoice /* csr 2490 tmp_ih_inv_nbr */
                       column-label "Inv #" format "x(06)"
                   with frame c.
 
              if first-of(tmp_ih_ship) then do:
                 if page-size - line-counter < 5 then page.
                 display with frame c.
                 if cur_cnvrsn = "" then      /*05/02/01*/
                    put "Customer: "
                         tmp_ih_cust
                        " "
                         cm_sort.
                 else                         /*05/02/01*/
                    put "Customer: "          /*05/02/01*/
                         tmp_ih_cust          /*05/02/01*/
                        " "                   /*05/02/01*/
                         cm_sort              /*05/02/01*/
                        "    "                /*05/02/01*/
                        "Currency: "          /*05/02/01*/
                         cur_cnvrsn.          /*05/02/01*/
 
                 find sp_mstr where sp_mstr.sp_domain = global_domain
                                and sp_addr = tmp_ih_slspsn   /*sh 11/96*/
                              no-lock no-error.
                 if available sp_mstr then do:                    /*sh 11/96*/
                    chr2 = string(sp_pos).                        /*sh 11/96*/
                    pos = chr2.                                   /*sh 11/96*/
                    put " Salesperson("
                            pos                                   /*sh 11/96*/
                           ") is "
                            sp_addr                               /*sh 11/96*/
                           " - "
                            sp_sort.
                 end.
 
              /* put "Currency: "               05/02/01*/
              /*      cur_cnvrsn                05/02/01*/
                 put  skip                    /*05/02/01*/
                     " Ship-to: "
                      tmp_ih_ship
                     " "
                      shipname
                      skip(1).
              end.
 
              accumulate tmp_idh_qty_inv (total by tmp_ih_ship).
           /* accumulate base_price (total by tmp_ih_ship). */    /*ss051206*/
 
              if page-size - line-counter < 5 then page.
 

            {us/bbi/gprun.i ""ktinvformat.p""
            "(input tmp_ih_inv_nbr, input disp_inv_len, output zinvoice)"}

              display tmp_ih_nbr
                      tmp_idh_site                                /*dsj030608*/
                      tmp_ih_po
                      tmp_ih_ship_date
                      tmp_idh_part
                      tmp_idh_custpart
                      tmp_idh_qty_inv
                      base_price
                      tmp_idh_type
                      tmp_ih_shipvia
                      tmp_ih_bol
                      zinvoice  /* csr 2490 tmp_ih_inv_nbr   */
                      with frame c.
 
           /* Begin added section 05/02/01 */
              find first so_mstr where so_mstr.so_domain = global_domain
                                   and so_nbr = tmp_ih_nbr no-lock no-error.
              if available so_mstr then do:
                if so_channel <> "Standard" and so_channel <> "" then do:
                  find first code_mstr where code_domain  = global_domain
                                         and code_fldname = "so_channel"
                                         and code_value   = so_channel
                                       no-lock no-error.
                  if available code_mstr then do:
                      put unformatted
                          so_channel at 2
                          code_cmmt at 11 /*skip*/.               /*tkt 9382*/
                  end.
                end.
              end.
           /* End added section   05/02/01 */
 
              /*tkt 9382 - Added */
              if tmp_idh__chr09 <> "" then                         
                 put unformatted "Release# " at 43               
                     tmp_idh__chr09 at 52 format "x(15)" skip.
              else put skip.
              
              if last-of(tmp_ih_ship) then do:
                 if page-size - line-counter < 5 then page.
                 underline tmp_idh_qty_inv
/*ss051206*/            /* base_price */                                    
                           with frame c.
                 display  "Ship-To Total:" @ tmp_idh_custpart
                           accum total by tmp_ih_ship (tmp_idh_qty_inv)
                                          @ tmp_idh_qty_inv
                               /*format "->,>>>,>>9"*/          /*dsj030608*/
                                 format "->>>,>>9"              /*dsj030608*/
/*ss051206*/            /* accum total by tmp_ih_ship (base_price)
/*sh 02-98*/                             @ base_price format "->,>>>,>>9.99" */
                           with frame c.
              end.
 
              if last(tmp_ih_ship) then do:
                 if page-size - line-counter < 5 then page.
                 underline tmp_idh_qty_inv
/*ss051206*/            /* base_price */
                           with frame c.
                 display  "  Report Total:" @ tmp_idh_custpart
                           accum total tmp_idh_qty_inv @ tmp_idh_qty_inv
                               /*format "->,>>>,>>9"*/          /*dsj030608*/
                                 format "->>>,>>9"              /*dsj030608*/
/*ss051206*/            /* accum total base_price @ base_price
/*sh 02-98*/                                       format "->,>>>,>>9.99" */
                           with frame c.
              end.
           end. /* else not wksheet */                            /*ss051206*/
       end. /* for each t_cust */
    end. /* if sort_by = 2 */
 
    if sort_by = "3" then
    do:
        for each tmpRecord no-lock
           break by tmp_ih_cust by tmp_ih__chr05 by tmp_ih_ship_date
              BY tmp_whs_sn
               with frame d width 132 no-box:
 
           filecontents = yes.                                    /*ss051106*/
 
           assign base_price = tmp_idh_price
                  cur_cnvrsn = "".
 
           if base_curr <> tmp_ih_curr and base_rpt = "Base" then
              assign base_price = base_price / tmp_ih_ex_rate
                     cur_cnvrsn = tmp_ih_curr.
 
           find cm_mstr where cm_mstr.cm_domain = global_domain
                          and cm_addr = tmp_ih_cust no-lock no-error.
 
           shipname = "".
           find ad_mstr where ad_mstr.ad_domain = global_domain
                          and ad_addr = tmp_ih_ship no-lock no-error.
           if available ad_mstr then shipname = ad_sort.
           IF AVAILABLE cm_mstr THEN DO:
            IF shipname = ""     THEN shipname = cm_sort.
           END.
 
           desc1 = "".                                /*ss092011 - Begin add */
           desc2 = "".
           find first pt_mstr where pt_domain = global_domain
                               and pt_part = tmp_idh_part no-lock no-error.
           if available pt_mstr
              then assign desc1 = pt_desc1
                          desc2 = pt_desc2.           /*ss092011 - End add */
 
                                                 /*csr 2490 */
          {us/bbi/gprun.i ""ktinvformat.p""
              "(input tmp_ih_inv_nbr, input disp_inv_len, output zinvoice)"}

           if wksheet then do:                        /*ss051206 - Begin add */
              export delimiter ","
                 tmp_ih_cust cm_sort shipname tmp_ih_nbr tmp_ih_po 
                 tmp_ih_ship_date
                 tmp_idh_part tmp_idh_custpart 
                 desc1 when show_desc = yes                       /*ss092011*/
                 desc2 when show_desc = yes                       /*ss092011*/
                 tmp_idh_qty_inv  WHEN tmp_ih_po <> ""
                 ""               WHEN tmp_ih_po =  ""
                 base_price       WHEN tmp_ih_po <> "" 
                                    and noprice_rpt = no          /*TKT53560*/
                 ""               WHEN tmp_ih_po =  ""
                                    and noprice_rpt = no          /*TKT53560*/
                 caps(tmp_idh_type) tmp_ih_shipvia tmp_ih_bol 
                 tmp_ih_inv_nbr
                 tmp_idh_line  WHEN tmp_ih_po <> ""       /*csr 2581*/ /*LN*/
                 ""            WHEN tmp_ih_po =  ""
                 tmp_idh_site                             /*TKT58518*/
                 tmp_ih__qad03 WHEN tmp_ih_po <> "" /*csr 2627*/     /*Boxes*/
                 ""            WHEN tmp_ih_po =  ""
                 tmp_ih__dec01 WHEN tmp_ih_po <> ""   /*csr 2627*/ /*Pallets*/
                 ""            WHEN tmp_ih_po =  ""
                 tmp_idh__chr09  /*tkt 9382*/
                 caps(tmp_whs_sn)  /*#39153*/ /*tkt23434*/ /*Serial Number*/
                 tmp_whs_type
                 tmp_wht_date_ship
                 tmp_wht_sn.
 
             end.
           else do:                                   /*ss051206 - End add */
              form tmp_ih_nbr /*at 2*/ label "SO #"               /*dsj030608*/
                   tmp_idh_site     label "Si" format "x(02)"     /*dsj030608*/
                   tmp_ih__chr05   label "NC Acct" format "x(16)"
                   tmp_ih_ship_date
                   tmp_idh_part     format "x(16)"
                   tmp_idh_qty_inv  column-label "Qty!Shipped" format "->>>,>>9"
                   tmp_idh_type
                   tmp_ih_po
                   zinvoice /* csr 2490 tmp_ih_inv_nbr */
                       column-label "Inv #" format "x(06)"
/*05/02/01*/       base_price
               with frame d.
 
              if first-of(tmp_ih_cust) then do:
                 if page-size - line-counter < 5 then page.
                 display with frame d.
                 if cur_cnvrsn = "" then
                    put "Customer: "
                         tmp_ih_cust
                        " "
                         cm_sort.
                 else
                    put "Customer: "
                         tmp_ih_cust
                        " "
                         cm_sort
                        "    "
                        "Currency: "
                         cur_cnvrsn.
 
                 find sp_mstr where sp_mstr.sp_domain = global_domain
                                and sp_addr = tmp_ih_slspsn   /*sh 11/96*/
                              no-lock no-error.
                 if available sp_mstr then do:                    /*sh 11/96*/
                    chr2 = string(sp_pos).                        /*sh 11/96*/
                    pos = chr2.                                   /*sh 11/96*/
                    put " Salesperson("
                            pos                                   /*sh 11/96*/
                           ") is "
                            sp_addr                               /*sh 11/96*/
                           " - "
                            sp_sort.
                 end.
                 put skip(1).
              end.
 
              accumulate tmp_idh_qty_inv (total by tmp_ih_cust).
           /* accumulate base_price (total by tmp_ih_cust). */     /*ss051206*/
 
              if page-size - line-counter < 5 then page.
 

         {us/bbi/gprun.i ""ktinvformat.p""
             "(input tmp_ih_inv_nbr, input disp_inv_len, output zinvoice)"}

              display tmp_ih_nbr
                      tmp_idh_site                                /*dsj030608*/
                      tmp_ih__chr05
                      tmp_ih_ship_date
                      tmp_idh_part
                      tmp_idh_qty_inv
                      tmp_idh_type
                      tmp_ih_po
                      zinvoice  /* tmp_ih_inv_nbr   csr 2490 */
/*05/02/01*/          base_price
                      with frame d.
 
           /* Begin added section 05/02/01 */
              find first so_mstr where so_mstr.so_domain = global_domain
                                   and so_nbr = tmp_ih_nbr no-lock no-error.
              if available so_mstr then do:
                if so_channel <> "Standard" and so_channel <> "" then do:
                  find first code_mstr where code_domain  = global_domain
                                         and code_fldname = "so_channel"
                                         and code_value   = so_channel
                                       no-lock no-error.
                  if available code_mstr then do:
                      put unformatted
                          so_channel at 2
                          code_cmmt at 11 /*skip*/.               /*tkt 9382*/
                  end.
                end.
              end.
           /* End added section   05/02/01 */
 
              /*tkt 9382 - Added */
              if tmp_idh__chr09 <> "" then                         
                 put unformatted "Release# " at 47               
                     tmp_idh__chr09 at 56 format "x(15)" skip.
              else put skip.
              
              if last-of(tmp_ih_cust) then do:
                 if page-size - line-counter < 5 then page.
                 underline tmp_idh_qty_inv
/*ss051206*/            /* base_price */
                           with frame d.
                 display  "Customer Total:" @ tmp_idh_part
                           accum total by tmp_ih_cust (tmp_idh_qty_inv)
                                          @ tmp_idh_qty_inv
                               /*format "->,>>>,>>9"*/          /*dsj030608*/
                                 format "->>>,>>9"              /*dsj030608*/
/*ss051206*/            /* accum total by tmp_ih_cust (base_price)
/*sh 02-98*/                             @ base_price format "->,>>>,>>9.99" */
                           with frame d.
              end.
 
              if last(tmp_ih_cust) then do:
                 if page-size - line-counter < 5 then page.
                 underline tmp_idh_qty_inv
/*ss051206*/            /* base_price */
                           with frame d.
                 display  "  Report Total:" @ tmp_idh_part
                           accum total tmp_idh_qty_inv @ tmp_idh_qty_inv
                               /*format "->,>>>,>>9"*/          /*dsj030608*/
                                 format "->>>,>>9"              /*dsj030608*/
/*ss051206*/            /* accum total base_price @ base_price
/*sh 02-98*/                                       format "->,>>>,>>9.99" */
                           with frame d.
              end.
           end. /* else not wksheet */
       end. /* for each t_cust */
    end. /* if sort_by = 3 */
 
    if sort_by = "4" then
    do:
        for each tmpRecord no-lock
           break by tmp_ih_cust by tmp_idh_part by tmp_ih_ord_date
            BY tmp_whs_sn
               with frame e width 132 no-box:
 
           filecontents = yes.                                    /*ss051106*/
           assign base_price = tmp_idh_price
                  cur_cnvrsn = "".
 
           if base_curr <> tmp_ih_curr and base_rpt = "Base" then
              assign base_price = base_price / tmp_ih_ex_rate
                     cur_cnvrsn = tmp_ih_curr.
 
           ext_price = base_price * tmp_idh_qty_inv.
 
           find cm_mstr where cm_mstr.cm_domain = global_domain
                          and cm_addr = tmp_ih_cust no-lock no-error.
 
           shipname = "".
           find ad_mstr where ad_mstr.ad_domain = global_domain
                          and ad_addr = tmp_ih_ship no-lock no-error.
           if available ad_mstr then shipname = ad_sort.
           IF AVAILABLE cm_mstr THEN DO:
            IF shipname = ""     THEN shipname = cm_sort.
           END.
 
           desc1 = "".                                /*ss092011 - Begin add */
           desc2 = "".
           find first pt_mstr where pt_domain = global_domain
                               and pt_part = tmp_idh_part no-lock no-error.
           if available pt_mstr
              then assign desc1 = pt_desc1
                          desc2 = pt_desc2.           /*ss092011 - End add */
 

          {us/bbi/gprun.i ""ktinvformat.p""
               "(input tmp_ih_inv_nbr, input disp_inv_len, output zinvoice)"}


           if wksheet then do:                        /*ss051206 - Begin add */
              export delimiter ","
                 tmp_ih_cust cm_sort shipname tmp_ih_nbr tmp_ih_po 
                 tmp_ih_ship_date
                 tmp_idh_part 
                 tmp_idh_custpart 
                 desc1 when show_desc = yes                       /*ss092011*/
                 desc2 when show_desc = yes                       /*ss092011*/
                 tmp_idh_qty_inv  WHEN tmp_ih_po <> ""
                 ""               WHEN tmp_ih_po =  ""
                 base_price       WHEN tmp_ih_po <> "" 
                                    and noprice_rpt = no          /*TKT53560*/
                 ""               WHEN tmp_ih_po =  ""
                                    and noprice_rpt = no          /*TKT53560*/
                 caps(tmp_idh_type) tmp_ih_shipvia tmp_ih_bol 
                 tmp_ih_inv_nbr
                 tmp_idh_line  WHEN tmp_ih_po <> ""       /*csr 2581*/ /*LN*/
                 ""            WHEN tmp_ih_po =  ""
                 tmp_idh_site                             /*TKT58518*/
                 tmp_ih__qad03 WHEN tmp_ih_po <> "" /*csr 2627*/     /*Boxes*/
                 ""            WHEN tmp_ih_po =  ""
                 tmp_ih__dec01 WHEN tmp_ih_po <> ""   /*csr 2627*/ /*Pallets*/
                 ""            WHEN tmp_ih_po =  ""
                 tmp_idh__chr09  /*tkt 9382*/
                 caps(tmp_whs_sn) /*#39153*/ /*tkt23434*/ /*Serial Number*/
                 tmp_whs_type
                 tmp_wht_date_ship
                 tmp_wht_sn.
             
            end.
           else do:                                   /*ss051206 - End add */
              form tmp_ih_nbr /*at 2*/ label "SO #"               /*dsj030608*/
                   tmp_idh_site     label "Si" format "x(02)"     /*dsj030608*/
                   tmp_ih_po
                   tmp_ih_ord_date
                   tmp_ih_ship_date
                   tmp_idh_part     format "x(16)"
                   tmp_idh_qty_inv  column-label "Qty!Shipped" 
                                    format "->>>>,>>9"
                   base_price
                   ext_price    format "->,>>>,>>9.99"
                   tmp_idh_type
                   zinvoice  /* csr 2490 tmp_ih_inv_nbr */
                      column-label "Inv #" format "x(06)"
                   with frame e.
 
              if first-of(tmp_ih_cust) then do:
                 if page-size - line-counter < 5 then page.
                 display with frame e.
                 if cur_cnvrsn = "" then
                    put "Customer: "
                         tmp_ih_cust
                        " "
                         cm_sort.
                 else
                    put "Customer: "
                         tmp_ih_cust
                        " "
                         cm_sort
                        "    "
                        "Currency: "
                         cur_cnvrsn.
 
                 find sp_mstr where sp_mstr.sp_domain = global_domain
                                and sp_addr = tmp_ih_slspsn   /*sh 11/96*/
                              no-lock no-error.
                 if available sp_mstr then do:                    /*sh 11/96*/
                    chr2 = string(sp_pos).                        /*sh 11/96*/
                    pos = chr2.                                   /*sh 11/96*/
                    put " Salesperson("
                            pos                                   /*sh 11/96*/
                           ") is "
                            sp_addr                               /*sh 11/96*/
                           " - "
                            sp_sort.
                 end.
                 put skip(1).
              end.
 
              accumulate tmp_idh_qty_inv (total by tmp_ih_cust).
           /* accumulate base_price  (total by tmp_ih_cust). */    /*ss051206*/
              accumulate ext_price   (total by tmp_ih_cust).
 
              if page-size - line-counter < 5 then page.
 

            {us/bbi/gprun.i ""ktinvformat.p""
                  "(input tmp_ih_inv_nbr, input disp_inv_len, output zinvoice)"}

              display tmp_ih_nbr
                      tmp_idh_site                               /*dsj030608*/
                      tmp_ih_po
                      tmp_ih_ord_date
                      tmp_ih_ship_date
                      tmp_idh_part
                      tmp_idh_qty_inv
                      base_price
                      ext_price
                      tmp_idh_type
                      zinvoice  /* csr 2490 tmp_ih_inv_nbr  */
                      with frame e.
 
           /* Begin added section 05/02/01 */
              find first so_mstr where so_mstr.so_domain = global_domain
                                   and so_nbr = tmp_ih_nbr no-lock no-error.
              if available so_mstr then do:
                if so_channel <> "Standard" and so_channel <> "" then do:
                  find first code_mstr where code_domain  = global_domain
                                         and code_fldname = "so_channel"
                                         and code_value   = so_channel
                                       no-lock no-error.
                  if available code_mstr then do:
                      put unformatted
                          so_channel at 2
                          code_cmmt at 11 /*skip*/.               /*tkt 9382*/
                  end.
                end.
              end.
           /* End added section   05/02/01 */
 
              /*tkt 9382 - Added */
              if tmp_idh__chr09 <> "" then                         
                 put unformatted "Release# " at 59               
                     tmp_idh__chr09 at 68 format "x(15)" skip.
              else put skip.
              
              if last-of(tmp_ih_cust) then do:
                 if page-size - line-counter < 5 then page.
                 underline tmp_idh_qty_inv
/*ss051206*/            /* base_price */
                           ext_price
                           with frame e.
                 display  "Customer Total:" @ tmp_idh_part
                           accum total by tmp_ih_cust (tmp_idh_qty_inv)
                                       @ tmp_idh_qty_inv
                               /*format "->,>>>,>>9"*/          /*dsj030608*/
                                 format "->>>,>>9"              /*dsj030608*/
/*ss051206*/            /* accum total by tmp_ih_cust (base_price)
/*sh 02-98*/                           @ base_price format "->,>>>,>>9.99" */
                           accum total by tmp_ih_cust (ext_price)
                                       @ ext_price format "->,>>>,>>9.99"
                           with frame e.
              end.
 
              if last(tmp_ih_cust) then do:
                 if page-size - line-counter < 5 then page.
                 underline tmp_idh_qty_inv
/*ss051206*/            /* base_price */
                           ext_price
                           with frame e.
                 display  "  Report Total:" @ tmp_idh_part
                           accum total tmp_idh_qty_inv @ tmp_idh_qty_inv
                               /*format "->,>>>,>>9"*/          /*dsj030608*/
                                 format "->>>,>>9"              /*dsj030608*/
/*ss051206*/            /* accum total base_price @ base_price
/*sh 02-98*/                                       format "->,>>>,>>9.99" */
                           accum total ext_price @ ext_price
                                                   format "->,>>>,>>9.99"
                           with frame e.
              end.
           end. /* else not wksheet */
       end. /* for each t_cust */
    end. /* sort_by = 4 */
 
    if sort_by = "5" then
    do:
        for each tmpRecord no-lock
        break by tmp_ih_cust by tmp_ih_ship_date by tmp_ih_nbr descending
         by tmp_idh_line BY tmp_whs_sn
               with frame f width 132 no-box:
 
           filecontents = yes.                                    /*ss051106*/
           assign base_price = tmp_idh_price
                  cur_cnvrsn = "".
 
           if base_curr <> tmp_ih_curr and base_rpt = "Base" then
              assign base_price = base_price / tmp_ih_ex_rate
                     cur_cnvrsn = tmp_ih_curr.
 
           ext_price = base_price * tmp_idh_qty_inv.
 
           find cm_mstr where cm_mstr.cm_domain = global_domain
                          and cm_addr = tmp_ih_cust no-lock no-error.
 
           shipname = "".
           find ad_mstr where ad_mstr.ad_domain = global_domain
                          and ad_addr = tmp_ih_ship no-lock no-error.
           if available ad_mstr then shipname = ad_sort.
           IF AVAILABLE cm_mstr THEN DO:
            IF shipname = ""     THEN shipname = cm_sort.
           END.
 
           desc1 = "".                                /*ss092011 - Begin add */
           desc2 = "".
           find first pt_mstr where pt_domain = global_domain
                               and pt_part = tmp_idh_part no-lock no-error.
           if available pt_mstr
              then assign desc1 = pt_desc1
                          desc2 = pt_desc2.           /*ss092011 - End add */
 

            {us/bbi/gprun.i ""ktinvformat.p""
               "(input tmp_ih_inv_nbr, input disp_inv_len, output zinvoice)"}


           if wksheet then do:                        /*ss051206 - Begin add */
              export delimiter ","
                 tmp_ih_cust cm_sort shipname tmp_ih_nbr tmp_ih_po 
                 tmp_ih_ship_date
                 tmp_idh_part tmp_idh_custpart 
                 desc1 when show_desc = yes                       /*ss092011*/
                 desc2 when show_desc = yes                       /*ss092011*/
                 tmp_idh_qty_inv  WHEN tmp_ih_po <> ""
                 ""               WHEN tmp_ih_po =  ""
                 base_price       WHEN tmp_ih_po <> "" 
                                    and noprice_rpt = no          /*TKT53560*/
                 ""               WHEN tmp_ih_po =  ""
                                    and noprice_rpt = no          /*TKT53560*/
                 caps(tmp_idh_type) tmp_ih_shipvia tmp_ih_bol 
                 tmp_ih_inv_nbr
                 tmp_idh_line  WHEN tmp_ih_po <> ""       /*csr 2581*/ /*LN*/
                 ""            WHEN tmp_ih_po =  ""
                 tmp_idh_site                             /*TKT58518*/
                 tmp_ih__qad03 WHEN tmp_ih_po <> "" /*csr 2627*/     /*Boxes*/
                 ""            WHEN tmp_ih_po =  ""
                 tmp_ih__dec01 WHEN tmp_ih_po <> ""   /*csr 2627*/ /*Pallets*/
                 ""            WHEN tmp_ih_po =  ""
                 tmp_idh__chr09  /*tkt 9382*/
                 caps(tmp_whs_sn) /*#39153*/ /*tkt23434*/ /*Serial Number*/
                 tmp_whs_type
                 tmp_wht_date_ship
                 tmp_wht_sn.
             
            end.
           else do:                                   /*ss051206 - End add */
              form tmp_ih_ship_date label "Shipped"
                   tmp_idh_site     label "Si" format "x(02)"
                   tmp_ih_nbr       label "SO #"
                   tmp_idh_line     label "Ln" format ">>9"       /*ss092011*/
                   tmp_ih_po        label "PO" format "x(12)"
                   tmp_idh_part     format "x(17)"
                   tmp_idh_qty_inv  column-label "Qty!Invoiced" format "->>>>>9"
                   base_price   format "->>>>>9.99"
                   ext_price    format "->>,>>9.99"
                   tmp_idh_type
                   tmp_ih__chr01    label "Shipping Acct" format "x(15)"
/*05/02/01*/       ad_sort      label "Ship-to"       format "x(20)"
                   with frame f.
 
              if first-of(tmp_ih_cust) then do:
                 if page-size - line-counter < 5 then page.
                 display with frame f.
                 if cur_cnvrsn = "" then
                    put "Customer: "
                         tmp_ih_cust
                        " "
                         cm_sort.
                 else
                    put "Customer: "
                         tmp_ih_cust
                        " "
                         cm_sort
                        "    "
                        "Currency: "
                         cur_cnvrsn.
 
                 find sp_mstr where sp_mstr.sp_domain = global_domain
                                and sp_addr = tmp_ih_slspsn       /*sh 11/96*/
                              no-lock no-error.
                 if available sp_mstr then do:                    /*sh 11/96*/
                    chr2 = string(sp_pos).                        /*sh 11/96*/
                    pos = chr2.                                   /*sh 11/96*/
                    put " Salesperson("
                            pos                                   /*sh 11/96*/
                           ") is "
                            sp_addr                               /*sh 11/96*/
                           " - "
                            sp_sort.
                 end.
                 put skip(1).
              end.
 
              accumulate tmp_idh_qty_inv (total by tmp_ih_cust).
           /* accumulate base_price  (total by tmp_ih_cust). */    /*ss051206*/
              accumulate ext_price   (total by tmp_ih_cust).
 
              find ad_mstr where ad_mstr.ad_domain = global_domain
                             and ad_addr = tmp_ih_ship no-lock no-error.
 
              if page-size - line-counter < 5 then page.
 
              display tmp_ih_ship_date
                      tmp_ih_nbr
                      tmp_idh_line
                      tmp_idh_site
                      tmp_ih_po
                      tmp_idh_part
                      tmp_idh_qty_inv
                      base_price
                      ext_price
                      tmp_idh_type
                      tmp_ih__chr01
                      ad_sort
                      with frame f.
 
           /* Begin added section 05/02/01 */
              find first so_mstr where so_mstr.so_domain = global_domain
                                   and so_nbr = tmp_ih_nbr no-lock no-error.
              if available so_mstr then do:
                if so_channel <> "Standard" and so_channel <> "" then do:
                  find first code_mstr where code_domain  = global_domain
                                         and code_fldname = "so_channel"
                                         and code_value   = so_channel
                                       no-lock no-error.
                  if available code_mstr then do:
                      put unformatted
                          so_channel at 13
                          code_cmmt at 22 /*skip*/.               /*tkt 9382*/
                  end.
                end.
              end.
           /* End added section   05/02/01 */
 
              /*tkt 9382 - Added */
              if tmp_idh__chr09 <> "" then                         
                 put unformatted "Release# " at 45               
                     tmp_idh__chr09 at 54 format "x(15)" skip.
              else put skip.
              
              if last-of(tmp_ih_cust) then do:
                 if page-size - line-counter < 5 then page.
                 underline tmp_idh_qty_inv
/*ss051206*/            /* base_price */
                           ext_price
                           with frame f.
                 display  "Customer Total:" @ tmp_idh_part
                           accum total by tmp_ih_cust (tmp_idh_qty_inv)
                                       @ tmp_idh_qty_inv format "->,>>>,>>9"
/*ss051206*/            /* accum total by tmp_ih_cust (base_price)
/*sh 02-98*/                           @ base_price format "->,>>>,>>9.99" */
                           accum total by tmp_ih_cust (ext_price)
                                       @ ext_price format "->,>>>,>>9.99"
                           with frame f.
              end.
 
              if last(tmp_ih_cust) then do:
                 if page-size - line-counter < 5 then page.
                 underline tmp_idh_qty_inv
/*ss051206*/            /* base_price */
                           ext_price
                           with frame f.
                 display  "  Report Total:" @ tmp_idh_part
                           accum total tmp_idh_qty_inv @ tmp_idh_qty_inv
                                                   format "->,>>>,>>9"
/*ss051206*/            /* accum total base_price @ base_price
/*sh 02-98*/                                       format "->,>>>,>>9.99" */
                           accum total ext_price @ ext_price
                                                   format "->,>>>,>>9.99"
                           with frame f.
              end.
           end. /* else not wksheet */
       end. /* for each t_cust */
    end. /* if sort_by = 5 */
 
    /****  jmh 02/09  *************/
    if sort_by = "6" then
    do:
        for each tmpRecord no-lock
           break by tmp_ih_cust by tmp_ih_po descending  
           by tmp_ih_ship_date BY tmp_whs_sn
           by tmp_ih_nbr  by tmp_idh_line
               with frame g width 132 no-box:
 
           filecontents = yes.                                    /*ss051106*/
           assign base_price = tmp_idh_price
                  cur_cnvrsn = "".
 
           if base_curr <> tmp_ih_curr and base_rpt = "Base" then
              assign base_price = base_price / tmp_ih_ex_rate
                     cur_cnvrsn = tmp_ih_curr.
 
           ext_price = base_price * tmp_idh_qty_inv.
 
           find cm_mstr where cm_mstr.cm_domain = global_domain
                          and cm_addr = tmp_ih_cust no-lock no-error.
 
           shipname = "".
           find ad_mstr where ad_mstr.ad_domain = global_domain
                          and ad_addr = tmp_ih_ship no-lock no-error.
           if available ad_mstr then shipname = ad_sort.
           IF AVAILABLE cm_mstr THEN DO:
            IF shipname = ""     THEN shipname = cm_sort.
           END.
 
           desc1 = "".                                /*ss092011 - Begin add */
           desc2 = "".
           find first pt_mstr where pt_domain = global_domain
                               and pt_part = tmp_idh_part no-lock no-error.
           if available pt_mstr
              then assign desc1 = pt_desc1
                          desc2 = pt_desc2.           /*ss092011 - End add */
 
                                                  /*csr 2490 */
            {us/bbi/gprun.i ""ktinvformat.p""
               "(input tmp_ih_inv_nbr, input disp_inv_len, output zinvoice)"}

           if wksheet then do:                        /*ss051206 - Begin add */
              export delimiter ","
                 tmp_ih_cust cm_sort shipname tmp_ih_nbr tmp_ih_po 
                 tmp_ih_ship_date
                 tmp_idh_part tmp_idh_custpart 
                 desc1 when show_desc = yes                       /*ss092011*/
                 desc2 when show_desc = yes                       /*ss092011*/
                 tmp_idh_qty_inv  WHEN tmp_ih_po <> ""
                 ""               WHEN tmp_ih_po =  ""
                 base_price       WHEN tmp_ih_po <> "" 
                                    and noprice_rpt = no          /*TKT53560*/
                 ""               WHEN tmp_ih_po =  ""
                                    and noprice_rpt = no          /*TKT53560*/
                 caps(tmp_idh_type) tmp_ih_shipvia tmp_ih_bol 
                 zinvoice /* csr 2490 tmp_ih_inv_nbr*/
                 tmp_idh_line  WHEN tmp_ih_po <> ""       /*csr 2581*/ /*LN*/
                 ""            WHEN tmp_ih_po =  ""
                 tmp_idh_site                             /*TKT58518*/
                 tmp_ih__qad03 WHEN tmp_ih_po <> "" /*csr 2627*/     /*Boxes*/
                 ""            WHEN tmp_ih_po =  ""
                 tmp_ih__dec01 WHEN tmp_ih_po <> ""   /*csr 2627*/ /*Pallets*/
                 ""            WHEN tmp_ih_po =  ""
                 tmp_idh__chr09  /*tkt 9382*/
                 caps(tmp_whs_sn) /*#39153*/ /*tkt23434*/ /*Serial Number*/
                 tmp_whs_type
                 tmp_wht_date_ship
                 tmp_wht_sn.
             
            end.
           else do:                                   /*ss051206 - End add */
              form tmp_ih_ship_date label "Shipped"
                   tmp_idh_site     label "Si" format "x(02)"
                   tmp_ih_nbr       label "SO #"
                   tmp_idh_line     label "Ln" format ">>9"       /*ss092011*/
                   tmp_ih_po        label "PO" format "x(12)"
                   tmp_idh_part     format "x(17)"
                   tmp_idh_qty_inv  column-label "Qty!Invoiced" format "->>>>>9"
                   base_price   format "->>>>>9.99"
                   ext_price    format "->>,>>9.99"
                   tmp_idh_type
                   tmp_idh_due_date   label "Due Date"       /*06/09 jmh*/
/*05/02/01*/       ad_sort      label "Ship-to"       format "x(15)"
                   zinvoice /*csr 2490 tmp_ih_inv_nbr*/ 
                          label "Inv #"  format "x(6)"
                               /*06/09 jmh*/
                   with frame g.
 
              if first-of(tmp_ih_cust) then do:
                 if page-size - line-counter < 5 then page.
                 display with frame g.
                 if cur_cnvrsn = "" then
                    put "Customer: "
                         tmp_ih_cust
                        " "
                         cm_sort.
                 else
                    put "Customer: "
                         tmp_ih_cust
                        " "
                         cm_sort
                        "    "
                        "Currency: "
                         cur_cnvrsn.
 
                 find sp_mstr where sp_mstr.sp_domain = global_domain
                                and sp_addr = tmp_ih_slspsn   /*sh 11/96*/
                              no-lock no-error.
                 if available sp_mstr then do:                    /*sh 11/96*/
                    chr2 = string(sp_pos).                        /*sh 11/96*/
                    pos = chr2.                                   /*sh 11/96*/
                    put " Salesperson("
                            pos                                   /*sh 11/96*/
                           ") is "
                            sp_addr                               /*sh 11/96*/
                           " - "
                            sp_sort.
                 end.
                 put skip(1).
              end.
 
              accumulate tmp_idh_qty_inv (total by tmp_ih_cust).
           /* accumulate base_price  (total by tmp_ih_cust). */    /*ss051206*/
              accumulate ext_price   (total by tmp_ih_cust).
 
              find ad_mstr where ad_mstr.ad_domain = global_domain
                             and ad_addr = tmp_ih_ship no-lock no-error.
 
              if page-size - line-counter < 5 then page.
 
                                              /*csr 2490*/
           {us/bbi/gprun.i ""ktinvformat.p""
                "(input tmp_ih_inv_nbr, input disp_inv_len, output zinvoice)"}

              display tmp_ih_ship_date
                      tmp_ih_nbr
                      tmp_idh_line
                      tmp_idh_site
                      tmp_ih_po
                      tmp_idh_part
                      tmp_idh_qty_inv
                      base_price
                      ext_price
                      tmp_idh_type
                      tmp_idh_due_date                   /*06/09 jmh*/
                      ad_sort
                      zinvoice   /*tmp_ih_inv_nbr 2490 */        /*06/09 jmh*/
                      with frame g.
 
           /* Begin added section 05/02/01 */
 
              find first so_mstr where so_mstr.so_domain = global_domain
                                   and so_nbr = tmp_ih_nbr no-lock no-error.
              if available so_mstr then do:
                if so_channel <> "Standard" and so_channel <> "" then do:
                  find first code_mstr where code_domain  = global_domain
                                         and code_fldname = "so_channel"
                                         and code_value   = so_channel
                                       no-lock no-error.
                  if available code_mstr then do:
                      put unformatted
                          so_channel at 13
                          code_cmmt at 22 /*skip*/.               /*tkt 9382*/
                  end.
                end.
              end.
           /* End added section   05/02/01 */
 
              /*tkt 9382 - Added */
              if tmp_idh__chr09 <> "" then                         
                 put unformatted "Release# " at 46               
                     tmp_idh__chr09 at 55 format "x(15)" skip.
              else put skip.
              
              if last-of(tmp_ih_cust) then do:
                 if page-size - line-counter < 5 then page.
                 underline tmp_idh_qty_inv
/*ss051206*/            /* base_price */
                           ext_price
                           with frame g.
                 display  "Customer Total:" @ tmp_idh_part
                           accum total by tmp_ih_cust (tmp_idh_qty_inv)
                                       @ tmp_idh_qty_inv format "->,>>>,>>9"
/*ss051206*/            /* accum total by tmp_ih_cust (base_price)
/*sh 02-98*/                           @ base_price format "->,>>>,>>9.99" */
                           accum total by tmp_ih_cust (ext_price)
                                       @ ext_price format "->,>>>,>>9.99"
                           with frame g.
              end.
 
              if last(tmp_ih_cust) then do:
                 if page-size - line-counter < 5 then page.
                 underline tmp_idh_qty_inv
/*ss051206*/            /* base_price */
                           ext_price
                           with frame g.
                 display  "  Report Total:" @ tmp_idh_part
                           accum total tmp_idh_qty_inv @ tmp_idh_qty_inv
                                                   format "->,>>>,>>9"
/*ss051206*/            /* accum total base_price @ base_price
/*sh 02-98*/                                       format "->,>>>,>>9.99" */
                           accum total ext_price @ ext_price
                                                   format "->,>>>,>>9.99"
                           with frame g.
              end.
           end. /* else not wksheet */
       end. /* for each t_cust */
    end. /* if sort_by = 6 */

    if sort_by = "7" or sort_by = "9"                       /*TKT53193*/
    or sort_by = "15"                                       //TKT14017
    then         /*csr 2627*/
    do:
        for each tmpRecord no-lock
        break by tmp_ih_cust by tmp_ih_ship_date by tmp_ih_nbr by tmp_idh_line
           descending   by tmp_ih_po by tmp_whs_sn
               with frame h width 132 no-box:
 

           filecontents = yes.                                    /*ss051106*/
           assign base_price = tmp_idh_price
                  cur_cnvrsn = "".
 
           if base_curr <> tmp_ih_curr and base_rpt = "Base" then
              assign base_price = base_price / tmp_ih_ex_rate
                     cur_cnvrsn = tmp_ih_curr.
 
           ext_price = base_price * tmp_idh_qty_inv.
 
           find cm_mstr where cm_mstr.cm_domain = global_domain
                          and cm_addr = tmp_ih_cust no-lock no-error.
 
           shipname = "".
           find ad_mstr where ad_mstr.ad_domain = global_domain
                          and ad_addr = tmp_ih_ship no-lock no-error.
           if available ad_mstr then shipname = ad_sort.
           IF AVAILABLE cm_mstr THEN DO:
            IF shipname = ""     THEN shipname = cm_sort.
           END.
 
           desc1 = "".                                /*ss092011 - Begin add */
           desc2 = "".
           find first pt_mstr where pt_domain = global_domain
                               and pt_part = tmp_idh_part no-lock no-error.
           if available pt_mstr
              then assign desc1 = pt_desc1
                          desc2 = pt_desc2.           /*ss092011 - End add */
 

            {us/bbi/gprun.i ""ktinvformat.p""
               "(input tmp_ih_inv_nbr, input disp_inv_len, output zinvoice)"}

               
           if wksheet then do:                        /*ss051206 - Begin add */
              export delimiter ","
                 tmp_ih_cust cm_sort shipname tmp_ih_nbr tmp_ih_po 
                 tmp_ih_ship_date
                 tmp_idh_part tmp_idh_custpart 
                 desc1 when show_desc = yes                       /*ss092011*/
                 desc2 when show_desc = yes                       /*ss092011*/
                 tmp_idh_qty_inv  WHEN tmp_ih_po <> ""
                 ""               WHEN tmp_ih_po =  ""
                 base_price       WHEN tmp_ih_po <> "" 
                                    and noprice_rpt = no          /*TKT53560*/
                 ""               WHEN tmp_ih_po =  ""
                                    and noprice_rpt = no          /*TKT53560*/
                 caps(tmp_idh_type) tmp_ih_shipvia tmp_ih_bol 
                 tmp_ih_inv_nbr
                 tmp_idh_line  WHEN tmp_ih_po <> ""       /*csr 2581*/ /*LN*/
                 ""            WHEN tmp_ih_po =  ""
                 tmp_idh_site                             /*TKT58518*/
                 tmp_ih__qad03 WHEN tmp_ih_po <> "" /*csr 2627*/     /*Boxes*/
                 ""            WHEN tmp_ih_po =  ""
                 tmp_ih__dec01 WHEN tmp_ih_po <> ""   /*csr 2627*/ /*Pallets*/
                 ""            WHEN tmp_ih_po =  ""
                 tmp_idh__chr09   /*tkt 9382*/
                 caps(tmp_whs_sn) /*#39153*/ /*tkt23434*/ /*Serial Number*/
                 tmp_whs_type
                 tmp_wht_date_ship
                 tmp_wht_sn
                 tmp_kit_sn when kit_exists = yes                 /*TKT53193*/
                 tmp_ih_rmks                          /* TKT43801 */
                 tmp_rev when sort_by = "15"                        //TKT18218
                 tmp_mfg_date                                       //TKT18218
                    when sort_by = "15" and tmp_mfg_date <> ?       //TKT18218
                 tmp_wo_lot when sort_by = "15".                    //TKT23984   
           end.
           else do:                                   /*ss051206 - End add */
              form tmp_ih_ship_date label "Shipped"
                   ad_sort  label "Ship-To" format "x(20)"
                   tmp_idh_site     label "Si" format "x(02)"
                   tmp_ih_nbr       label "SO #"
                   tmp_idh_line     label "Ln" format ">>9"       /*ss092011*/
                   tmp_ih_po        label "PO" format "x(12)"
                   tmp_idh_part     format "x(17)"
                   desc1            format "x(18)"
                   tmp_idh_qty_inv  column-label "Qty!Invoiced" format "->>>>>9"
                   tmp_idh_type
                   tmp_ih__qad03  label "Boxes" format ">>>>>>>"  /*csr 2627*/
                   tmp_ih__dec01 format ">>>>>" label "Pallets" /*csr 2627*/
                   with frame h.
 
              if first-of(tmp_ih_cust) then do:
                 if page-size - line-counter < 5 then page.
                 display with frame h.
                 if cur_cnvrsn = "" then
                    put "Customer: "
                         tmp_ih_cust
                        " "
                         cm_sort.
                 else
                    put "Customer: "
                         tmp_ih_cust
                        " "
                         cm_sort
                        "    "
                        "Currency: "
                         cur_cnvrsn.
 
                 find sp_mstr where sp_mstr.sp_domain = global_domain
                                and sp_addr = tmp_ih_slspsn       /*sh 11/96*/
                              no-lock no-error.
                 if available sp_mstr then do:                    /*sh 11/96*/
                    chr2 = string(sp_pos).                        /*sh 11/96*/
                    pos = chr2.                                   /*sh 11/96*/
                    put " Salesperson("
                            pos                                   /*sh 11/96*/
                           ") is "
                            sp_addr                               /*sh 11/96*/
                           " - "
                            sp_sort.
                 end.
                 put skip(1).
              end.
 
              accumulate tmp_idh_qty_inv (total by tmp_ih_cust).
              accumulate ext_price   (total by tmp_ih_cust).
 
              find ad_mstr where ad_mstr.ad_domain = global_domain
                             and ad_addr = tmp_ih_ship no-lock no-error.
 
              if page-size - line-counter < 5 then page.
 
              display tmp_ih_ship_date
                      ad_sort
                      tmp_ih_nbr
                      tmp_idh_line
                      tmp_idh_site
                      tmp_ih_po
                      tmp_idh_part
                      desc1
                      tmp_idh_qty_inv
                      tmp_ih__qad03
                      tmp_ih__dec01
                      tmp_idh_type
                      with frame h.
 
           /* Begin added section 05/02/01 */
              find first so_mstr where so_mstr.so_domain = global_domain
                                   and so_nbr = tmp_ih_nbr no-lock no-error.
              if available so_mstr then do:
                if so_channel <> "Standard" and so_channel <> "" then do:
                  find first code_mstr where code_domain  = global_domain
                                         and code_fldname = "so_channel"
                                         and code_value   = so_channel
                                       no-lock no-error.
                  if available code_mstr then do:
                      put unformatted
                          so_channel at 13
                          code_cmmt at 22 /*skip*/.               /*tkt 9382*/
                  end.
                end.
              end.
           /* End added section   05/02/01 */
 
              /*tkt 9382 - Added */
              if tmp_idh__chr09 <> "" then                         
                 put unformatted "Release# " at 40               
                     tmp_idh__chr09 at 49 format "x(15)" skip.
              else put skip.
              
              if last-of(tmp_ih_cust) then do:
                 if page-size - line-counter < 5 then page.
                 underline tmp_idh_qty_inv
                           with frame h.
                 display  "Customer Total:" @ tmp_idh_part
                           accum total by tmp_ih_cust (tmp_idh_qty_inv)
                                       @ tmp_idh_qty_inv format "->,>>>,>>9"
                           with frame h.
              end.
 
              if last(tmp_ih_cust) then do:
                 if page-size - line-counter < 5 then page.
                 underline tmp_idh_qty_inv
                           with frame h.
                 display  "  Report Total:" @ tmp_idh_part
                           accum total tmp_idh_qty_inv @ tmp_idh_qty_inv
                                                   format "->,>>>,>>9"
                           with frame h.
              end.
           end. /* else not wksheet */
       end. /* for each t_cust */
    end. /* if sort_by = 7 */
 
    if sort_by = "8" then                       /*TKT26885 - begin add*/
    do:
        /* update cust part with long cust part detail */
        for each tmpRecord share-lock:
            find first cp_mstr where cp_domain = global_domain
                                and cp_part = tmp_idh_part
                                and cp_cust_part = tmp_idh_custpart
                                            no-lock no-error.
            if available cp_mstr and cp_cust_partd <> "" 
            then tmp_idh_custpart = cp_cust_partd.
        end.
                 for each tmpRecord share-lock
        where tmp_whs_sn <> ""
        break by tmp_ih_po by tmp_idh_custpart
              by tmp_whs_sn
               with frame i width 132 no-box:

           filecontents = yes.

           find first cp_mstr where cp_domain = global_domain
                                and cp_part = tmp_idh_part
                                and cp_cust_part = tmp_idh_custpart
                                            no-lock no-error.

            /*tkt27151 start*/
           shipname = "".
           find ad_mstr where ad_mstr.ad_domain = global_domain
                          and ad_addr = tmp_ih_ship no-lock no-error.
           if available ad_mstr then shipname = ad_sort.
           IF AVAILABLE cm_mstr THEN DO:
            IF shipname = ""     THEN shipname = cm_sort.
           END.
          /*tkt27151 end*/
 
 
           if available cp_mstr and cp_cust_partd <> "" 
           then tmp_idh_custpart = cp_cust_partd.

           if wksheet then do:
              export delimiter ","
                 tmp_ih_po 
                 tmp_idh_custpart
                 caps(tmp_whs_sn)      /*#39153*/
                 tmp_ih_ship_date.
           end. /* wksheet */
           else do:
              form tmp_ih_ship_date label "Shipped"
                   shipname         label "Ship-To" format "x(20)"
                   tmp_idh_site     label "Si" format "x(02)"
                   tmp_ih_nbr       label "SO #"
                   tmp_idh_line     label "Ln" format ">>9"
                   tmp_ih_po        label "PO" format "x(12)"
                   tmp_ih_ship_date 
                   tmp_idh_custpart
                   tmp_whs_sn
                   with frame i.
 
              display tmp_ih_ship_date
                      ad_sort
                      tmp_idh_site
                      tmp_ih_nbr
                      tmp_idh_line
                      tmp_ih_po
                      tmp_ih_ship_date
                      tmp_idh_custpart
                      caps(tmp_whs_sn)  /*#39153*/
                      with frame i.
               
           end. /* else not wksheet */
       end. /* for each tmpRecord */
    end. /* if sort_by = 8 */                       /*TKT26885 - end add*/
    
    /*TKT62597 Begin Add*/
    if sort_by = "10" then do:
        for each tmpRecord no-lock
        break by tmp_ih_cust by tmp_ih_ship_date by tmp_ih_nbr by tmp_idh_line
           descending   by tmp_ih_po by tmp_whs_sn:
 
           filecontents = yes.                                    /*ss051106*/
 
           {us/bbi/gprun.i ""ktinvformat.p""
               "(input tmp_ih_inv_nbr, input disp_inv_len, output zinvoice)"}

           palletid      = "".
           hid_ship_date = "".
           hid_ship_date = string(YEAR(date(tmp_ih_ship_date)),"9999") 
                               + "-" + string(MONTH(date(tmp_ih_ship_date)),"99") 
                               + "-" + string(DAY(date(tmp_ih_ship_date)),"99").
           
           if tmp_wht_sn <> "" then
           find first wh_structure where whs_sn = tmp_wht_sn
                                     and whs_parent <> ""
                                     and whs_type <> "Pallet_delete"
                                     and whs_type <> "TEMP"
                                     and whs_type <> "SHIPPING"
                                     no-lock no-error.
           if available wh_structure then
           find first wh_tracking where wht_sn = whs_parent 
                                    and wht_so = tmp_ih_nbr
                                    and wht_so_line = tmp_idh_line
                                    and wht_model <> ""
                                    and wht_labeltype <> "TEMP"
                                    and wht_labeltype <> "SHIPPING"
                                    no-lock no-error.
                                                                            
           if available wh_tracking then
               palletid = wht_sn.
           else do:
               if available wh_structure then 
                   palletid = whs_parent.
           end.        
           
           if wksheet then do:                        
              put unformatted
                 tmp_whs_sn ','
                 tmp_idh_custpart ','
                 tmp_ih_po ','
                 tmp_wht_sn ','
                 palletid ','
                 tmp_ih_inv_nbr ','
                 hid_ship_date ','
                 tmp_ih_bol
                 skip.
           end.
       end. /* for each t_cust */
    end. /* if sort_by = 10 */
    /*TKT62597 End Add*/
 
    /*TKT67043 Begin Add*/
    if sort_by = "11" then do:
        
        for each tmpRecord no-lock
            break by tmp_ih_cust by tmp_ih_bol by tmp_ih_ship_date by tmp_ih_nbr by tmp_idh_line
        descending   by tmp_ih_po by tmp_whs_sn:
        
            filecontents = yes.
             
            /*palletid = "".*/ /*TKT69899*/
            length   = "".
            width    = "".
            height   = "".
            weight   = "".
            
            if tmp_wht_qty <> 0 then .
            else next.
            
            find first pt_mstr where pt_domain = global_domain
                                 and pt_part = tmp_idh_part no-lock no-error.
                                 
            if available pt_mstr then
                assign length = string(pt_length)
                       width  = string(pt_width)
                       height = string(pt_height)
                       weight = string(pt_net_wt).                
            
            if length = "0" then length = "".
            if width = "0" then width = "".
            if height = "0" then height = "".
            if weight = "0" then weight = "".
            
            /* TKT69899 - Pallet ID was replaced with BOX ID 
             * if tmp_wht_sn <> "" then
             * find first wh_structure where whs_sn = tmp_wht_sn
             *                         and whs_parent <> ""
             *                         and whs_type <> "Pallet_delete"
             *                         and whs_type <> "TEMP"
             *                         and whs_type <> "SHIPPING"
             *                         no-lock no-error.
             * if available wh_structure then
             * find first wh_tracking where wht_sn = whs_parent 
             *                        and wht_so = tmp_ih_nbr
             *                        and wht_so_line = tmp_idh_line
             *                        and wht_model <> ""
             *                        and wht_labeltype <> "TEMP"
             *                        and wht_labeltype <> "SHIPPING"
             *                        no-lock no-error.
             *                                                               
             * if available wh_tracking then
             * palletid = wht_sn.
             * else do:
             *   if available wh_structure then 
             *       palletid = whs_parent.
             * end. */
            
            if wksheet then do:
                
                put unformatted
                    tmp_ih_bol        ',' /*bol number*/
                    ""                ',' /*purchase order*/
                    ""                ',' /*shipcarrier*/
                    ""                ',' /*notes*/
                    tmp_idh_part      ',' /*SKU*/
                    "1"               ',' /*Quantity*/ /*tmp_wht_qty replaced TKT69899*/
                    ""                ',' /*lot#*/
                    tmp_whs_sn        ',' /*Serial Number*/
                    ""                ',' /*expiration date*/
                    ""                ',' /*locationfield1*/
                    ""                ',' /*locationfield2*/
                    ""                ',' /*locationfield3*/
                    ""                ',' /*locationfield4*/
                    ""                ',' /*cost*/
                    ""                ',' /*Var UOM Avg*/
                    ""                ',' /*Receipt Advice Number*/
                    ""                ',' /*TrackingNumber*/
                    ""                ',' /*SupplierCompanyName*/
                    tmp_wht_sn        ','. /*BoxID*/ /*TKT69899*/
                    /*palletid          ','.*/       /*TKT69899*/
                
                /* if palletid <> "" then put unformatted
                 *   "Pallet"          ','.   TKT69899*/
                if tmp_wht_sn <> "" then put unformatted
                    "Pallet"             ','.   /*TKT69899*/ 
                else put unformatted
                    ""                ','.

                put unformatted
                    length            ',' /*MUlength*/
                    width             ',' /*MUwidth*/
                    height            ',' /*MUheight*/
                    weight            ',' /*MUweight*/
                    ""                ',' /*Receiver Transaction Saved Element*/
                    ""                    /*CreateMultipleMUs*/
                    skip.
                    
            end. /*if wksheet*/
        end. /*for each tmpRecord*/
    end. /*if sort_by = "11"*/     
    
    /*TKT78432 Begin Add*/
    if sort_by = "12" then do:
        
        for each tmpRecord no-lock
            break by tmp_ih_cust by tmp_ih_ship_date descending by tmp_ih_nbr by tmp_idh_line:
        
            filecontents = yes.
            
            desc1 = "".                              
            
            find first pt_mstr where pt_domain = global_domain
                               and pt_part = tmp_idh_part no-lock no-error.
            
            if available pt_mstr
                then assign desc1 = pt_desc1 + " " + pt_desc2.
                                 
            if wksheet then do:
                
                put unformatted
                    tmp_ih_po         ',' /*Purchase Order*/
                    tmp_idh_line      ',' /*Line*/
                    tmp_ih_nbr        ',' /*Order*/
                    tmp_ih_ship_date  ',' /*Shipped Date*/
                    tmp_idh_per_date  ',' /*Original Commit*/
                    tmp_ih_ord_date   ',' /*Order Date*/
                    tmp_idh_part      ',' /*Item Number*/
                    '"' desc1 '"'     ',' /*Description*/
                    tmp_idh_qty       ',' /*Qty Ordered*/
                    tmp_ih_shipvia    ',' /*Ship Via*/
                    tmp_ih_bol            /*BOL*/
                    skip
                    . 
                    
            end. /*if wksheet*/
        end. /*for each tmpRecord*/
    end. /*if sort_by = "12"*/
    /*TKT78432 End Add*/

    /*TKT03145 begin add*/
    if sort_by = "13" then do:
        for each tmpRecord no-lock break by tmp_ih_cust  by tmp_ih_ship_date by tmp_ih_nbr by tmp_idh_line:

            filecontents = yes.

            find cm_mstr where cm_mstr.cm_domain = global_domain
            and cm_addr = tmp_ih_cust no-lock no-error.

            shipname = "".
            find ad_mstr where ad_mstr.ad_domain = global_domain
                        and ad_addr = tmp_ih_ship no-lock no-error.
            if available ad_mstr then shipname = ad_sort.
            if available cm_mstr then do:
                if shipname = "" then shipname = cm_sort.
            end.

            palletid = "".
            if tmp_wht_sn <> "" then
            find first wh_structure where whs_sn = tmp_wht_sn
                                        and whs_parent <> ""
                                        and whs_type <> "Pallet_delete"
                                        and whs_type <> "TEMP"
                                        and whs_type <> "SHIPPING"
                                        no-lock no-error.
            if available wh_structure then
            find first wh_tracking where wht_sn = whs_parent 
                                        and wht_so = tmp_ih_nbr
                                        and wht_so_line = tmp_idh_line
                                        and wht_model <> ""
                                        and wht_labeltype <> "TEMP"
                                        and wht_labeltype <> "SHIPPING"
                                        no-lock no-error.
                                                                                
            if available wh_tracking then
                palletid = wht_sn.
            else do:
                if available wh_structure then 
                    palletid = whs_parent.
            end. 

            find first pt_mstr where pt_domain = global_domain
                and pt_part = tmp_idh_part no-lock no-error.

            if available pt_mstr then
            assign
                desc1 = pt_desc1
                desc2 = pt_desc2.
            else assign
                desc1 = ""
                desc2 = "".
            
            if wksheet then 
            put unformatted 
                tmp_ih_cust ztab
                cm_sort ztab
                shipname ztab
                tmp_ih_nbr ztab
                tmp_ih_po ztab
                tmp_ih_ship_date ztab
                "1" ztab // level
                tmp_idh_part ztab
                '' ztab // sub item no
                '' ztab // sub serial no
                tmp_idh_custpart ztab 
                desc1 ztab
                desc2 ztab
                tmp_idh_qty ztab
                tmp_idh_price ztab
                '' ztab
                tmp_ih_shipvia ztab
                tmp_ih_bol ztab
                tmp_ih_inv_nbr ztab
                tmp_idh_line ztab
                tmp_idh_site ztab
                tmp_whs_sn ztab
                '' ztab
                '' ztab
                palletid
                skip.

            for each wh_structure no-lock where whs_parent = tmp_whs_sn
                                            and whs_type = "KIT":

                find first pt_mstr where pt_domain = global_domain
                    and pt_part = whs__chr02 no-lock no-error.

                if available pt_mstr then
                assign
                    desc1 = pt_desc1
                    desc2 = pt_desc2.
                else assign
                    desc1 = ""
                    desc2 = "".

                if wksheet then
                put unformatted
                    '' ztab
                    '' ztab
                    '' ztab
                    '' ztab
                    '' ztab
                    '' ztab
                    "2" ztab // level
                    '' ztab
                    caps(whs__chr02) ztab
                    whs_sn ztab
                    '' ztab
                    desc1 ztab
                    desc2 ztab
                    '' ztab
                    '' ztab
                    '' ztab
                    '' ztab
                    tmp_ih_bol ztab
                    tmp_ih_inv_nbr ztab
                    '' ztab
                    '' ztab
                    tmp_whs_sn ztab
                    '' ztab
                    '' ztab
                    palletid
                    skip.
            end.
        end.
    end.
    /*TKT03145 end add*/

    /********************/
 
    /*Added code rws 4/02*/
    output close.
 
    if filecontents then do:
        
        /* Convert file to DOS format */
        if do_ftp = no then                                     /*dsj050608*/
            unix silent unix2dos -q -n value(attach + ".temp")  value(attach).
 
        /*Send the email*/
        //TKT18218 emailfrom = trim(global_userid) + "@REDACTED.com".
        
        /*Check to see if it's a real email address and added
          multiple email addresses added 4/22 by rws*/
        if index(emailto, chr(64)) = 0 and emailto <> ""
        and do_ftp = no                                         /*dsj050608*/
        then do:
            for each usrw_wkfl where usrw_wkfl.usrw_domain = global_domain
                                 and usrw_key1 = "distlist"
                                 and usrw_key2 = emailto
                               no-lock:
                do i = 1 to 15:
                    if usrw_charfld[i] <> "" then do:
/*srr121708*               value("~"Shipments Report from REDACTEDEMS"~"")   */
                        unix silent value("/apps/job/sendMail.p")
                            value(emailfrom)
                            value(usrw_charfld[i])
                            value(subject)                 /*srr121708*/
                            value("~""~"")                 /*jmh 03/04*/
                            value(attach).
                    end.
                end.
            end.
        end.
/*dsj050608 - add for FTP bit */
        else if do_ftp = yes then do:
            if lProdServer = true then do: //TKT18046 machine = "rh7qad2019" 
                /* set ux2dos on one line                       **cjw081612*/
                unix silent unix2dos -q -n value(dirnm[2] + fname + ".temp")  value(dirnm[2] + fname).
                // unix silent rm value(dirnm[2] + fname + ".temp"). /*TKT20306*/
                unix silent cp value(dirnm[2] + fname)
                               value(dirnm[1] + fname).
                // unix silent cp value(dirnm[2] + fname)  /*TKT20306*/
                //                value(dirnm[3] + fname). /*TKT20306*/
                // unix silent rm value(dirnm[2] + fname). /*TKT20306*/

                assign zpostfiles[1] = dirnm[2] + fname.   /*TKT20306*/
            end.
            
            /*TKT20306 Begin Add*/ 
            else do:
                unix silent unix2dos -q -n value(testdir + fname + ".temp")  value(testdir + fname).
                // unix silent rm value(testdir + fname + ".temp"). /*TKT20306*/

                assign zpostfiles[1] = testdir + fname.
            end.
 
            run postSFTP (input zprogram, input zpostfiles, input zpostdir, input ztest, input zconfirm, input zstagingdir, input zarchivedir[1], output fileExists, output sftpOutput).
            
            if zconfirm = true then do:
                if fileExists = false then do:
                    if trim(zErrorEmail) <> "" then do:
                        
                        assign 
                            zSubject = "~"SFTP (" + zprogram + ") Report Upload Confirmation Failed - File: " + fname + "~"".

                        if lProdServer then zAttach = dirnm[2] + fname + ".temp". 
                        else zAttach = testdir + fname + ".temp".
                            
                        if index(zErrorEmail, chr(64)) = 0 then do:
                            for each usrw_wkfl where usrw_wkfl.usrw_domain = global_domain
                                                and usrw_key1 = "distlist"
                                                and usrw_key2 = zErrorEmail
                                            no-lock:
                                do i = 1 to 15:
                                    if usrw_charfld[i] <> "" then do:
                                        unix silent value("/apps/job/sendMail.p")
                                            value(emailfrom)
                                            value(usrw_charfld[i])
                                            value(zSubject)                
                                            value(sftpOutput)
                                            value(zattach)
                                            .
                                    end. /* if usrw_charfld[i] */
                                end. /* do i = 1 to 15 */
                            end. /* for each usrw_wkfl */
                        end. /* if index(zErrorEmail) */
                        else do:
                            unix silent value("/apps/job/sendMail.p")
                                value(emailfrom)
                                value(zErrorEmail)
                                value(zSubject)                
                                value(sftpOutput)
                                value(zattach).
                        end.
                        os-delete value(sftpOutput).
                    end.
                end.
            end.

            if lProdServer then 
                unix silent rm value(dirnm[2] + fname + ".temp"). 
            else
                unix silent rm value(testdir + fname + ".temp").
            /*TKT20306 End Add*/
            
            /*{us/bbi/gprun.i ""ktso1280.p""}*/ /* Do the FTP bit */ /*TKT20306 Comment Out*/

        end.
/*dsj050608 end add */
        else if emailto <> "" then do:
/*srr121708*     value("~"Shipments Report from REDACTEDEMS"~"") *dsj092903*/
            unix silent value("/apps/job/sendMail.p")
                value(emailfrom)
                value(emailto)
                value(subject)                                   /*srr121708*/
                value("~""~"")
                value(attach).
        end.
        if do_ftp = no then do:                                 /*dsj050608*/
            unix silent rm value(attach + ".temp") > /dev/null.
            unix silent rm value(attach).
        end.                                                    /*dsj050608*/
    end.
 
    /*End added code rws 4/02*/
    {us/mf/mfrpexit.i}
    {us/mf/mfrtrail.i}
end.
 
 
procedure get_consign_qty:
    define buffer rcttrans for tr_hist.
    define output parameter consign_qty_ship like tr_qty_loc.
    consign_qty_ship = 0.
 
      for first rcttrans
         where rcttrans.tr_domain = global_domain
         and   rcttrans.tr_trnbr  = integer(tr_hist.tr_rmks)
         and   rcttrans.tr_type   = "RCT-TR"
         and   rcttrans.tr_nbr    = tr_hist.tr_nbr
      no-lock:
         consign_qty_ship = rcttrans.tr_qty_loc.
      end.  /* for first rcttrans */
 
end procedure. /* get_consign_qty */
 


procedure createTrRecord:
    def buffer whs for wh_structure.                            /*TKT53193*/

    //TKT71794 customer option to remove SN prefix
    //(done here in case of multiple customers)
    assign sn_prefix_char = ""
           sn_prefix_length = 0.
    {us/kt/ktcustoptc.i "t_cust.t_addr" "'Shipment_rpt_sn_prefix'" "sn_prefix_char"}
    sn_prefix_length = int(sn_prefix_char) no-error.
    if error-status:error then sn_prefix_length = 0.
    //TKT71794 end add

    find first so_mstr no-lock where so_domain = global_domain
                                 and so_nbr    = tr_hist.tr_nbr no-error.
    if available so_mstr then
    find first sod_det no-lock where sod_domain = global_domain
                                 and sod_nbr    = tr_hist.tr_nbr
                                 and sod_line   = tr_line no-error.
 
/*srr120909 - add*/
    if available so_mstr then do:
        if so_channel = "ADJ" then                           /*ss040411*/
           return.                                           /*ss040411*/
/*TKT28001*
*        if ((sochannel = "1" and so_channel = "RMA")        /*srr120909*/
*            or (sochannel = "2" and so_channel <> "RMA")    /*srr120909*/
*/
        if ((sochannel = "1" and so_channel begins "RMA")           /*TKT28001*/
            or (sochannel = "2" and not so_channel begins "RMA")    /*TKT28001*/
            or (sochannel = "3"))                           /*srr120909*/
        then .
        else return.
    end.

/*tkt23434*/                                      /* tkt 24777*/
    FIND FIRST wh_tracking WHERE wht_so     = so_nbr 
                        /*   AND wht_CustID = so_ship    tkt 58995*/
                             and wht_so_line = sod_line
                             AND wht_Model <> ''
                             NO-LOCK NO-ERROR.
 
    create tmpRecord.
    assign tmp_ih_nbr       = tr_hist.tr_nbr
           tmp_idh_line     = tr_line
           tmp_idh_site     = tr_site
           tmp_ih_ship_date = tr_effdate
           tmp_idh_part     = tr_part
           tmp_idh_qty_inv  = trqty
           tmp_idh_price    = tr_price
           tmp_idh_type     = tr_ship_type
           tmp_ih_curr      = tr_curr
           tmp_ih_ex_rate   = tr_ex_rate
           tmp_ih_cust      = tr_addr
           tmp_ih_slspsn    = tr_slspsn[sp_pos].
 
    if available sod_det then do:
        assign tmp_ih_ord_date  = so_ord_date
               tmp_idh_custpart = sod_custpart
               tmp_ih_po        = so_po
               tmp_ih_shipvia   = so_shipvia
               tmp_idh_due_date = sod_due_date        /*06/09 jmh*/
               tmp_idh_per_date = sod_per_date        /*TKT78432*/ 
               tmp_wht_date_ship = ""
               tmp_ih_bol       = so_bol
               tmp_ih_inv_nbr   = so_inv_nbr 
               tmp_ih_ship      = so_ship
               tmp_ih__chr01    = so__chr01
               tmp_ih__chr05    = so__chr05
               tmp_idh__chr09   = sod__chr09.         /*tkt 9382*/
        if so__chr06 <> "" then tmp_ih_shipvia = so__chr06.     /*ss091009*/  
       
    end.

    
/*tkt23434*/
    IF AVAILABLE wh_tracking THEN DO:                 /*hdt 24777*/
     FOR EACH wh_tracking WHERE wht_so     = so_nbr 
                      /*    AND wht_CustId = so_ship tkt 58995  */
                            and wht_so_line = sod_line
                            AND wht_Model <> ''
                    no-lock,                                        /*TKT28473*/
       EACH wh_structure WHERE whs_parent = wht_sn
                    //TKT14121 include other types for sort 14
                    and (whs_type = "Packaging" or sort_by = "14")
                    no-lock:
                        //and whs_type = "Packaging" no-lock.         /*TKT28473*/
/*TKT28473*
       CREATE tmpRecord.
       ASSIGN tmp_ih_nbr       = tr_hist.tr_nbr
              tmp_idh_part     = tr_part
              tmp_ih_ship      = so_ship
              tmp_ih_ship_date = tr_effdate
              tmp_ih_cust      = tr_hist.tr_addr
              tmp_wht_date_ship = string(wht_date_shipped)
              tmp_wht_sn       = caps(wht_sn)  /*#39153*/
              tmp_whs_sn       = caps(whs_sn)  /*#39153*/
              tmp_whs_type     = whs_type.
              
              if tmp_wht_date_ship = "?" then tmp_wht_date_ship = "".
              */


/*TKT28473 - begin add*/
        find first tmpRecord where tmp_ih_nbr       = tr_hist.tr_nbr
              and tmp_idh_part     = tr_part
              and tmp_ih_ship      = so_ship
              and tmp_ih_ship_date = tr_effdate
              and tmp_ih_cust      = tr_hist.tr_addr 
              and tmp_wht_sn       = "" no-error.   /* no sn record */
        if not available tmpRecord then do:
/*TKT28473 - end add*/

           CREATE tmpRecord.
           ASSIGN tmp_ih_nbr       = tr_hist.tr_nbr
                  tmp_idh_part     = tr_part
                  tmp_ih_ship      = so_ship
                  tmp_ih_ship_date = tr_effdate
                  tmp_ih_cust      = tr_hist.tr_addr.

        end.    /*TKT28473*/

        assign tmp_wht_date_ship = string(wht_date_shipped)
              tmp_wht_sn       = caps(wht_sn)  /*#39153*/
              tmp_whs_sn       = caps(whs_sn)  /*#39153*/
              tmp_whs_type     = whs_type.
              
              if tmp_wht_date_ship = "?" then tmp_wht_date_ship = "".

              //TKT71794 remove sn prefix
              if sn_prefix_length > 0
              then tmp_whs_sn = substring(tmp_whs_sn, sn_prefix_length + 1).

        /*TKT53193 - add kit sn*/
        if sort_by = "9" then do:
            find first whs where whs.whs_sn = wh_structure.whs_sn
                             and whs.whs_type = "KIT" no-lock no-error.
            if available whs then assign kit_exists = yes
                                         tmp_kit_sn = caps(whs_parent).
                             else tmp_kit_sn = "".
        end. /* sort_by = "9" */

     END.
    END.
    
 
end procedure. /* createTrRecord*/
 
procedure createInvRecord:
    define input parameter lastRec as logical.                  /*TKT42799*/
    def var t_shipID like wht_shipID.                           /*TKT42799*/
    def buffer whs for wh_structure.                            /*TKT53193*/

    if sort_By = "2" 
    and (ih_hist.ih_ship <> shipto1 and shipto1 <> "") then next.

    //TKT71794 customer option to remove SN prefix
    //(done here in case of multiple customers)
    assign sn_prefix_char = ""
           sn_prefix_length = 0.
    {us/kt/ktcustoptc.i "t_cust.t_addr" "'Shipment_rpt_sn_prefix'" "sn_prefix_char"}
    sn_prefix_length = int(sn_prefix_char) no-error.
    if error-status:error then sn_prefix_length = 0.
    //TKT71794 end add

/*TKT42799 - begin add, find matching shipID*/
    release wh_tracking no-error.
    t_shipID = int(idh_hist.idh__chr08) no-error.

/*tkt23434*/                                     /*tkt 24777*/
    FIND FIRST wh_tracking WHERE wht_so     = ih_hist.ih_nbr
                       /*  AND wht_CustID = ih_hist.ih_ship tkt 58995 */
                            and wht_so_line = idh_hist.idh_line
                            AND wht_Model <> ''
                            AND ((wht_shipID = t_shipID and t_shipID <> 0)
                                        or t_shipID = 0)        /*TKT42799*/
                            NO-LOCK NO-ERROR.
    if available wh_tracking then
       find first wh_structure WHERE whs_parent = wht_sn
                    //TKT14121 include other types for sort 14
                    and (whs_type = "Packaging" or sort_by = "14")
                                 //TKT14121 and whs_type = "Packaging" 
                                 no-lock no-error.
 
    if (not available wh_tracking                                /*TKT42799*/
        or not available wh_structure)                           /*TKT42799*/
    and lastRec = yes
    then do:
        create tmpRecord.
        assign tmp_ih_nbr       = ih_hist.ih_nbr
               tmp_idh_line     = idh_hist.idh_line   /*line*/
               tmp_idh_site     = idh_site
               tmp_ih_po        = ih_po
               tmp_ih_ship_date = ih_ship_date
               tmp_ih_ord_date  = ih_ord_date
               tmp_idh_part     = idh_part
               tmp_idh_custpart = idh_custpart
               tmp_idh_qty_inv  = day_inv_qty /*idh_qty_inv */
               tmp_idh_price    = idh_price
               tmp_idh_type     = idh_type
               tmp_idh_due_date = idh_due_date        /*06/09 jmh*/
               tmp_idh_per_date = idh_per_date        /*TKT78432*/
               tmp_ih_shipvia   = ih_shipvia
               tmp_ih_bol       = ih_bol
               tmp_ih__qad03    = ih__qad03  /*boxes csr 2627*/  /*boxes*/
               tmp_ih__dec01    = ih__dec01  /* Skids or pallets  csr 2627*/
               tmp_ih_inv_nbr   = trim(mult_inv) /*ih_inv_nbr  */
               tmp_ih_curr      = ih_curr
               tmp_wht_date_ship = ""
               tmp_ih_ex_rate   = ih_ex_rate
               tmp_ih_cust      = ih_cust
               tmp_ih_ship      = ih_ship
               tmp_ih_slspsn    = ih_slspsn[sp_pos]
               tmp_ih__chr01    = ih__chr01
               tmp_ih__chr05    = ih__chr05
               tmp_idh__chr09   = idh__chr09         /*tkt 9382*/
               tmp_ih_rmks      = ih_rmks.
    end. /*TKT42799*/

                              
/*tkt23434*/                                     /*tkt 24777*/
    IF AVAILABLE wh_tracking THEN DO:
     FOR EACH wh_tracking WHERE wht_so     = ih_hist.ih_nbr
             /*             AND wht_CustId = ih_hist.ih_ship tkt 58995*/
                            AND wht_so_line = idh_hist.idh_line
                            AND wht_Model <> ''
                            AND ((wht_shipID = t_shipID and t_shipID <> 0)
                                        or t_shipID = 0)        /*TKT42799*/
            no-lock,                                                /*TKT28473*/
       EACH wh_structure WHERE whs_parent = wht_sn
                    //TKT14121 include other types for sort 14
                    and (whs_type = "Packaging" or sort_by = "14")
                        //TKT14121 and whs_type = "Packaging" 
                        no-lock:         /*TKT28473*/

/*TKT28473 - begin add*/
        find first tmpRecord where tmp_ih_nbr       = ih_hist.ih_nbr
              and replace(tmp_ih_inv_nbr, ";", "")  = ih_inv_nbr
              and tmp_idh_line     = idh_line
              and tmp_wht_sn       = "" no-error.   /* no sn record */
        if not available tmpRecord then do:
/*TKT28473 - end add*/

        //TKT18218 begin add
        assign t_rev = ""
               t_mfg_date = ?
               t_wo_lot = ?. //TKT23984
        find first kwo_snd where kwo_serial_nbr = whs_sn no-lock no-error.
        if available kwo_snd then do:
            find first wo_mstr where wo_domain = global_domain
                                and wo_lot = kwo_snd.kwo_lot no-lock no-error.
            if available wo_mstr
            then assign t_mfg_date = wo_rel_date
                        t_rev = wo_rev
                        t_wo_lot = wo_lot. //TKT23984
        end.                                            //TKT18218 end add

       CREATE tmpRecord.
       aSSIGN tmp_ih_nbr       = ih_hist.ih_nbr
              tmp_idh_line = idh_line
              tmp_idh_site     = idh_site
              tmp_ih_po        = ih_po
              tmp_ih_ship_date = ih_ship_date
              tmp_ih_ord_date  = ih_ord_date
              tmp_idh_part     = idh_part
              tmp_idh_custpart = idh_custpart
              tmp_idh_qty_inv  = idh_qty_inv    /*TKT42799* day_inv_qty*/
              tmp_idh_price    = idh_price
              tmp_idh_type     = idh_type
              tmp_idh_due_date = idh_due_date        /*06/09 jmh*/
              tmp_idh_per_date = idh_per_date        /*TKT78432*/
              tmp_ih_shipvia   = ih_shipvia
              tmp_ih_bol       = ih_bol
              tmp_ih__qad03    = ih__qad03  /*boxes csr 2627*/  /*boxes*/
              tmp_ih__dec01    = ih__dec01  /* Skids or pallets  csr 2627*/
              tmp_ih_inv_nbr   = ih_inv_nbr
              tmp_ih_curr      = ih_curr
              tmp_wht_date_ship = ""
              tmp_ih_ex_rate   = ih_ex_rate
              tmp_ih_cust      = ih_cust
              tmp_ih_ship      = ih_ship
              tmp_ih_slspsn    = ih_slspsn[sp_pos]
              tmp_ih__chr01    = ih__chr01
              tmp_ih__chr05    = ih__chr05
              tmp_idh__chr09   = idh__chr09         /*tkt 9382*/
              tmp_idh_part     = idh_part
              tmp_ih_ship      = ih_ship
              tmp_ih_ship_date = ih_ship_date
              tmp_ih_rmks      = ih_rmks
              tmp_wo_lot       = t_wo_lot           //TKT23984
              tmp_rev          = t_rev              //TKT18218
              tmp_mfg_date     = t_mfg_date.        //TKT18218

        end.            /*TKT28473*/
        assign  tmp_wht_date_ship = string(wht_date_shipped )
              tmp_ih_cust      = ih_cust
              tmp_wht_sn       = wht_sn
              tmp_whs_sn       = whs_sn
              tmp_wht_qty      = wht_boxqty      /*TKT67043*/
              tmp_whs_type     = whs_type.
         if tmp_wht_date_ship = "?" then tmp_wht_date_ship = "".

              //TKT71794 remove sn prefix
              if sn_prefix_length > 0
              then tmp_whs_sn = substring(tmp_whs_sn, sn_prefix_length + 1).

        /*TKT53193 - add kit sn*/
        if sort_by = "9" then do:
            find first whs where whs.whs_sn = wh_structure.whs_sn
                             and whs.whs_type = "KIT" no-lock no-error.
            if available whs then assign kit_exists = yes
                                         tmp_kit_sn = caps(whs_parent).
                             else tmp_kit_sn = "".
        end. /* sort_by = "9" */

     END.
    END.
   
end procedure. /* createInvRecord */
