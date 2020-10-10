import React, { useEffect, useState } from 'react'
import Paper from '@material-ui/core/Paper'
import TextField from '@material-ui/core/TextField'
import Autocomplete from '@material-ui/lab/Autocomplete'
import { Button, Table, TableBody, TableCell, TableHead, TableRow } from '@material-ui/core'
import IconButton from '@material-ui/core/IconButton'
import { EstudanteModel, initialStateEstudante } from '../model/estudante-model'
import AddIcon from '@material-ui/icons/Add'
import { makeStyles } from '@material-ui/core/styles'
import { addMatricula, getEstudanteById, getEstudantes } from '../services/pessoa-service'
import { useSnackbar } from 'notistack'
import { getCursos } from '../services/cursos-service'
import { v4 as uuidv4 } from 'uuid'
import {
  useHistory,
  useParams
} from 'react-router-dom'

const useStyles = makeStyles(() => ({
  fields: {
    margin: '10px'
  },
  cancelButton: {
    color: 'white'
  }
}))

const MatriculaPage: React.FC = () => {
  const history = useHistory()
  const { estudanteId } = useParams()
  const [estudante, setEstudante] = useState<EstudanteModel>()
  const [cursos, setCursos] = useState([])
  const [selectedCurso, setSelectedCurso] = useState<any>({})
  const [mode, setMode] = useState<'edit' | 'add' | 'none'>('none')
  const classes = useStyles()
  const { enqueueSnackbar } = useSnackbar()

  useEffect(() => {
    getEstudanteById(estudanteId).then((res: any) => {
      setEstudante(res.data)
    })

    getCursos().then((res: any) => {
      setCursos(res.data)
    })
  }, [estudanteId])

  const openAdd = () => {
    setMode('add')
  }

  const table = estudante?.matriculas ? <Table>
    <TableHead>
      <TableRow>
        <TableCell>Nome</TableCell>
        <TableCell align="right">
          <IconButton style={{ backgroundColor: '#2196f3' }} onClick={openAdd} aria-label="add">
            <AddIcon style={{ color: 'white' }} fontSize="large"/>
          </IconButton>
        </TableCell>
      </TableRow>
    </TableHead>
    <TableBody>
      {estudante.matriculas.map((matricula: any) => (
        <TableRow key={matricula?.id}>
          <TableCell component="th" scope="row">
            {matricula?.curso?.nome}
          </TableCell>
          <TableCell align="right">
            <Button variant="contained" color="primary" onClick={() => history.push(`/inscricao/${matricula.id}`)}>Inscrições</Button>
          </TableCell>
        </TableRow>
      ))}
    </TableBody>
  </Table> : <div> </div>

  const onSave = () => {
    const cmd = {
      pessoaId: estudante.id,
      cursoId: selectedCurso.id,
      matriculaId: uuidv4()
    }

    addMatricula(cmd).then(() => {
      getEstudanteById(estudanteId).then((res: any) => {
        setEstudante(res.data)
        enqueueSnackbar('Matricula criado com sucesso.', { variant: 'success' })
      })
    })
  }

  const onCancel = () => {
    setMode('none')
  }

  return (
    <Paper style={{ height: '80vh', marginLeft: '4vw', padding: '20px' }}>
      {estudante?.nome}

      {mode !== 'none'
        ? <div>
          <form noValidate autoComplete="off">

            <Autocomplete
              value={selectedCurso}
              onChange={(event: any, newValue: EstudanteModel | null) => {
                setSelectedCurso(newValue)
              }}
              id="combo-box-demo"
              options={cursos}
              getOptionLabel={(option) => option.nome}
              style={{ width: 300 }}
              renderInput={(params) => <TextField {...params} label="Curso" />}
            />

          </form>
          <Button className={classes.cancelButton} variant="contained" color="secondary" onClick={onCancel}>
            Cancel
          </Button>
          <Button variant="contained" color="primary" onClick={onSave}>
            Salvar
          </Button>
        </div>
        : null}

      {table}

    </Paper>
  )
}

export default MatriculaPage